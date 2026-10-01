namespace ApartmentManagementSystem.Models
{
    // Refresh tokens are stored as a SHA-256 hash, so a database leak doesn't expose usable tokens
    public class RefreshToken
    {
        public int Id { get; set; }                 // Primary key
        public string UserId { get; set; }          // Foreign key to AppUser
        public AppUser User { get; set; }           // Navigation property
        public string TokenHash { get; set; }       // SHA-256 of the token sent to the client
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }    // Set on logout, rotation, or reuse detection
    }
}
