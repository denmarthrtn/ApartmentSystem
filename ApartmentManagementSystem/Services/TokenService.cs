using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Options;
using ApartmentManagementSystem.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApartmentManagementSystem.Services
{
    public class TokenService : ITokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtOptions _options;
        private readonly TimeProvider _timeProvider;

        public TokenService(ApplicationDbContext context, IOptions<JwtOptions> options, TimeProvider timeProvider)
        {
            _context = context;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public (string Token, DateTime ExpiresAt) CreateAccessToken(AppUser user, IEnumerable<string> roles)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var expires = now.AddMinutes(_options.AccessTokenMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(r => new Claim("role", r)));
            if (user.TenantId.HasValue)
                claims.Add(new Claim(HttpCurrentUser.TenantIdClaim, user.TenantId.Value.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        public async Task<string> CreateRefreshTokenAsync(string userId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = Hash(token),
                CreatedAt = now,
                ExpiresAt = now.AddDays(_options.RefreshTokenDays)
            });
            await _context.SaveChangesAsync();

            return token; // only the hash is stored; the client keeps the real value
        }

        public async Task<string?> RedeemRefreshTokenAsync(string refreshToken)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == Hash(refreshToken));
            if (stored == null) return null;

            if (stored.RevokedAt != null)
            {
                // A used token came back: it was probably stolen. Log the user out everywhere.
                await RevokeAllForUserAsync(stored.UserId, now);
                return null;
            }

            if (stored.ExpiresAt <= now) return null;

            stored.RevokedAt = now; // single use: rotated on every refresh
            await _context.SaveChangesAsync();
            return stored.UserId;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == Hash(refreshToken));
            if (stored == null || stored.RevokedAt != null) return;

            stored.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _context.SaveChangesAsync();
        }

        private async Task RevokeAllForUserAsync(string userId, DateTime now)
        {
            var active = await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync();

            foreach (var token in active)
                token.RevokedAt = now;

            await _context.SaveChangesAsync();
        }

        private static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
