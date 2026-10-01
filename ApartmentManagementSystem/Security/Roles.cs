namespace ApartmentManagementSystem.Security
{
    public static class Roles
    {
        public const string Admin = "Admin";   // Landlord / property manager
        public const string Tenant = "Tenant"; // Can only see their own lease and bills
    }
}
