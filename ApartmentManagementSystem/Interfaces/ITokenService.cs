using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) CreateAccessToken(AppUser user, IEnumerable<string> roles);
        Task<string> CreateRefreshTokenAsync(string userId);

        // Returns the user id if the token is valid (and revokes it, since each refresh token is single-use).
        // Returns null if the token is unknown, expired, or was already used.
        Task<string?> RedeemRefreshTokenAsync(string refreshToken);

        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
