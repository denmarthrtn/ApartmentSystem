namespace ApartmentManagementSystem.Models
{
    public enum LeaseStatus
    {
        Active,
        Ended,
        Terminated
    }

    public class Lease
    {
        public int Id { get; set; }                  // Primary key
        public int TenantId { get; set; }            // Foreign key to Tenant
        public Tenant Tenant { get; set; }           // Navigation property
        public int ApartmentId { get; set; }         // Foreign key to Apartment
        public Apartment Apartment { get; set; }     // Navigation property
        public DateTime StartDate { get; set; }      // Lease start
        public DateTime EndDate { get; set; }        // Planned lease end
        public DateTime? TerminatedDate { get; set; } // Set when ended early
        public decimal MonthlyRent { get; set; }     // Agreed rent (can differ from Apartment.Rent)
        public decimal SecurityDeposit { get; set; } // Deposit held
        public LeaseStatus Status { get; set; } = LeaseStatus.Active;
    }
}
