using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.DTOs
{
    public class MaintenanceCreateDto
    {
        public int? ApartmentId { get; set; }   // Admin only; for tenants it comes from their active lease
        public string Title { get; set; }
        public string Description { get; set; }
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Normal;
    }

    public class MaintenanceStatusUpdateDto
    {
        public MaintenanceStatus Status { get; set; }
        public string? ResolutionNotes { get; set; }   // Required when resolving
    }

    // ?status=Open&priority=Urgent&page=1&pageSize=10
    public class MaintenanceQuery : PaginationQuery
    {
        public MaintenanceStatus? Status { get; set; }
        public MaintenancePriority? Priority { get; set; }
    }

    public class MaintenanceRequestDto
    {
        public int Id { get; set; }
        public int ApartmentId { get; set; }
        public string ApartmentName { get; set; }
        public int? TenantId { get; set; }
        public string TenantName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }
    }
}
