using Microsoft.AspNetCore.Identity;

namespace ApartmentManagementSystem.Models
{
    // Login account. Tenant accounts are linked to their Tenant record; admin accounts are not.
    public class AppUser : IdentityUser
    {
        public int? TenantId { get; set; }   // Foreign key to Tenant (null for admins)
        public Tenant? Tenant { get; set; }  // Navigation property
    }
}
