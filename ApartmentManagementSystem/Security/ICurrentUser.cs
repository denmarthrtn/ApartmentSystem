namespace ApartmentManagementSystem.Security
{
    // The logged-in user, as seen by the service layer (no dependency on HttpContext)
    public interface ICurrentUser
    {
        bool IsAdmin { get; }
        int? TenantId { get; }
    }

    public static class CurrentUserExtensions
    {
        // Admins can access everything; a tenant only their own records
        public static void EnsureCanAccessTenant(this ICurrentUser user, int tenantId)
        {
            if (user.IsAdmin) return;
            if (user.TenantId == tenantId) return;

            throw new UnauthorizedAccessException("You can only access your own records");
        }
    }
}
