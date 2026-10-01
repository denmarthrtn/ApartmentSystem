namespace ApartmentManagementSystem.Models
{
    public enum MaintenanceStatus
    {
        Open,
        InProgress,
        Resolved,
        Cancelled
    }

    // Stored as a number (not text) so the database can sort by it: Urgent first.
    // The API still shows it as text, e.g. "Urgent".
    public enum MaintenancePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }

    public class MaintenanceRequest
    {
        public int Id { get; set; }                        // Primary key
        public int ApartmentId { get; set; }               // Foreign key to Apartment
        public Apartment Apartment { get; set; }           // Navigation property
        public int? TenantId { get; set; }                 // Who reported it (null if logged by the admin)
        public Tenant? Tenant { get; set; }                // Navigation property
        public string Title { get; set; }                  // Short summary, e.g. "Leaking faucet"
        public string Description { get; set; }            // Details from the tenant
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Normal;
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }       // What was done to fix it

        // Allowed status changes. Resolved and Cancelled are final.
        public static bool CanMove(MaintenanceStatus from, MaintenanceStatus to) => (from, to) switch
        {
            (MaintenanceStatus.Open, MaintenanceStatus.InProgress) => true,
            (MaintenanceStatus.Open, MaintenanceStatus.Cancelled) => true,
            (MaintenanceStatus.InProgress, MaintenanceStatus.Resolved) => true,
            (MaintenanceStatus.InProgress, MaintenanceStatus.Open) => true, // e.g. waiting on parts, put back in the queue
            _ => false
        };
    }
}
