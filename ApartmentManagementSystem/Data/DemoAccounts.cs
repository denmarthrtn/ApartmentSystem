using ApartmentManagementSystem.Security;

namespace ApartmentManagementSystem.Data
{
    // Fixed accounts for trying out the API. These passwords are public on purpose,
    // so they are only created when "DemoMode:Enabled" is true (see IdentitySeeder).
    // Tenant IDs match the sample tenants in SeedData.
    public static class DemoAccounts
    {
        public record DemoAccount(string Email, string Password, string Role, int? TenantId);

        public static readonly IReadOnlyList<DemoAccount> All = new[]
        {
            new DemoAccount("demo.admin@apartments.local", "Demo#Admin2026", Roles.Admin, null),
            new DemoAccount("juan.delacruz@example.com", "Tenant#2026x", Roles.Tenant, 1001),
            new DemoAccount("maria.santos@example.com", "Tenant#2026x", Roles.Tenant, 1002),
            new DemoAccount("carlo.reyes@example.com", "Tenant#2026x", Roles.Tenant, 1003),
            new DemoAccount("ana.villanueva@example.com", "Tenant#2026x", Roles.Tenant, 1004)
        };
    }
}
