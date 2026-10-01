using System.IdentityModel.Tokens.Jwt;
using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Options;
using ApartmentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentManagementSystem.Tests
{
    public class TokenServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);

        private static JwtOptions Options => new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "test-signing-key-that-is-at-least-32-bytes-long!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };

        private TokenService GetService(out ApplicationDbContext context, DateTime? now = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
                .Options;

            context = new ApplicationDbContext(options);
            return new TokenService(context, Microsoft.Extensions.Options.Options.Create(Options), new FixedTimeProvider(now ?? Now));
        }

        private static AppUser TenantUser => new AppUser { Id = "user-1", Email = "juan@example.com", TenantId = 1001 };

        [Fact]
        public void CreateAccessToken_ShouldContainUserClaims()
        {
            var service = GetService(out _);

            var (token, _) = service.CreateAccessToken(TenantUser, new[] { "Tenant" });
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal("user-1", jwt.Subject);
            Assert.Equal("test-issuer", jwt.Issuer);
            Assert.Contains("test-audience", jwt.Audiences);
            Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "Tenant");
            Assert.Contains(jwt.Claims, c => c.Type == "tenantId" && c.Value == "1001");
            Assert.Contains(jwt.Claims, c => c.Type == "email" && c.Value == "juan@example.com");
        }

        [Fact]
        public void CreateAccessToken_AdminWithoutTenant_ShouldHaveNoTenantClaim()
        {
            var service = GetService(out _);
            var admin = new AppUser { Id = "admin-1", Email = "admin@example.com" };

            var (token, _) = service.CreateAccessToken(admin, new[] { "Admin" });
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.DoesNotContain(jwt.Claims, c => c.Type == "tenantId");
        }

        [Fact]
        public void CreateAccessToken_ShouldExpireAfterConfiguredMinutes()
        {
            var service = GetService(out _);

            var (token, expiresAt) = service.CreateAccessToken(TenantUser, new[] { "Tenant" });
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal(Now.AddMinutes(15), expiresAt);
            Assert.Equal(Now.AddMinutes(15), jwt.ValidTo);
        }

        [Fact]
        public async Task CreateRefreshTokenAsync_ShouldStoreOnlyHash()
        {
            var service = GetService(out var context);

            var token = await service.CreateRefreshTokenAsync("user-1");

            var stored = await context.RefreshTokens.SingleAsync();
            Assert.NotEqual(token, stored.TokenHash);
            Assert.Equal(Now.AddDays(7), stored.ExpiresAt);
        }

        [Fact]
        public async Task RedeemRefreshTokenAsync_ValidToken_ShouldReturnUserAndBeSingleUse()
        {
            var service = GetService(out var context);
            var token = await service.CreateRefreshTokenAsync("user-1");

            var userId = await service.RedeemRefreshTokenAsync(token);

            Assert.Equal("user-1", userId);
            Assert.NotNull((await context.RefreshTokens.SingleAsync()).RevokedAt);
        }

        [Fact]
        public async Task RedeemRefreshTokenAsync_ReusedToken_ShouldRevokeAllUserTokens()
        {
            var service = GetService(out var context);
            var stolen = await service.CreateRefreshTokenAsync("user-1");
            await service.RedeemRefreshTokenAsync(stolen);                  // legitimate use
            var newer = await service.CreateRefreshTokenAsync("user-1");    // the rotated token

            var reuse = await service.RedeemRefreshTokenAsync(stolen);       // attacker replays the old one

            Assert.Null(reuse);
            Assert.Null(await service.RedeemRefreshTokenAsync(newer));       // the newer token was revoked too
            Assert.All(await context.RefreshTokens.ToListAsync(), t => Assert.NotNull(t.RevokedAt));
        }

        [Fact]
        public async Task RedeemRefreshTokenAsync_ExpiredToken_ShouldReturnNull()
        {
            var service = GetService(out var context);
            var token = await service.CreateRefreshTokenAsync("user-1");

            // Same database, 8 days later
            var later = new TokenService(context, Microsoft.Extensions.Options.Options.Create(Options), new FixedTimeProvider(Now.AddDays(8)));

            Assert.Null(await later.RedeemRefreshTokenAsync(token));
        }

        [Fact]
        public async Task RedeemRefreshTokenAsync_UnknownToken_ShouldReturnNull()
        {
            var service = GetService(out _);

            Assert.Null(await service.RedeemRefreshTokenAsync("not-a-real-token"));
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ShouldPreventFurtherUse()
        {
            var service = GetService(out _);
            var token = await service.CreateRefreshTokenAsync("user-1");

            await service.RevokeRefreshTokenAsync(token);

            Assert.Null(await service.RedeemRefreshTokenAsync(token));
        }

        [Fact]
        public void JwtOptions_ShouldRejectShortSigningKey()
        {
            var options = Options;
            options.SigningKey = "too-short";

            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }
    }
}
