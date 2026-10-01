namespace ApartmentManagementSystem.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RefreshRequestDto
    {
        public string RefreshToken { get; set; }
    }

    public class TokenResponseDto
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; }
    }

    public class RegisterTenantDto
    {
        public int TenantId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
