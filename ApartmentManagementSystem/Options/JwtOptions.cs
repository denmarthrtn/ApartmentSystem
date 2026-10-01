namespace ApartmentManagementSystem.Options
{
    // Bound from the "Jwt" config section.
    // SigningKey is a secret: set it with dotnet user-secrets locally and an App Service setting / Key Vault in Azure.
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public string SigningKey { get; set; } = "";
        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 7;

        // HMAC-SHA256 needs a key of at least 256 bits
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(SigningKey) || System.Text.Encoding.UTF8.GetByteCount(SigningKey) < 32)
                throw new InvalidOperationException(
                    "Jwt:SigningKey is missing or shorter than 32 bytes. Set it with 'dotnet user-secrets set Jwt:SigningKey <value>' " +
                    "or as an app setting (Jwt__SigningKey) in Azure.");
            if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
                throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
        }
    }
}
