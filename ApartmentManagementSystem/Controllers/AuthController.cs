using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ApplicationDbContext _context;

        public AuthController(UserManager<AppUser> userManager, ITokenService tokenService, ApplicationDbContext context)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            // Same response for "no such user" and "wrong password", so emails can't be probed
            if (user == null) return Unauthorized();
            if (await _userManager.IsLockedOutAsync(user)) return Unauthorized();

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                await _userManager.AccessFailedAsync(user); // locks the account after repeated failures
                return Unauthorized();
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            return Ok(await IssueTokensAsync(user));
        }

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(RefreshRequestDto dto)
        {
            var userId = await _tokenService.RedeemRefreshTokenAsync(dto.RefreshToken);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || await _userManager.IsLockedOutAsync(user)) return Unauthorized();

            return Ok(await IssueTokensAsync(user));
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(RefreshRequestDto dto)
        {
            await _tokenService.RevokeRefreshTokenAsync(dto.RefreshToken);
            return NoContent();
        }

        // POST: api/auth/register-tenant
        // Admin creates a login for an existing tenant record
        [HttpPost("register-tenant")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> RegisterTenant(RegisterTenantDto dto)
        {
            if (!await _context.Tenants.AnyAsync(t => t.Id == dto.TenantId))
                throw new KeyNotFoundException($"Tenant {dto.TenantId} not found");
            if (await _context.Users.AnyAsync(u => u.TenantId == dto.TenantId))
                throw new InvalidOperationException($"Tenant {dto.TenantId} already has a login");

            var user = new AppUser { UserName = dto.Email, Email = dto.Email, TenantId = dto.TenantId };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new ArgumentException(string.Join(" ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, Roles.Tenant);
            return Ok(new { user.Id, user.Email, user.TenantId });
        }

        private async Task<TokenResponseDto> IssueTokensAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var (accessToken, expiresAt) = _tokenService.CreateAccessToken(user, roles);
            var refreshToken = await _tokenService.CreateRefreshTokenAsync(user.Id);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = expiresAt,
                RefreshToken = refreshToken
            };
        }
    }
}
