using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Data
{
    // Runs at startup:
    //  1. Makes sure the roles exist.
    //  2. Creates the real admin account from config (SeedAdmin:Email / SeedAdmin:Password), if set.
    //     That password comes from user-secrets locally or an App Service setting in Azure, never from source control.
    //  3. If DemoMode:Enabled is true, creates the fixed demo accounts in DemoAccounts.
    // Accounts that already exist are left alone, so this is safe to run on every startup.
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { Roles.Admin, Roles.Tenant })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            var adminEmail = config["SeedAdmin:Email"];
            var adminPassword = config["SeedAdmin:Password"];
            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
                await CreateUserAsync(userManager, adminEmail, adminPassword, Roles.Admin, null, logger);
            else
                logger.LogInformation("SeedAdmin not configured; skipping admin account creation");

            if (config.GetValue<bool>("DemoMode:Enabled"))
            {
                logger.LogWarning("Demo mode is on: creating demo accounts with public passwords");
                foreach (var demo in DemoAccounts.All)
                    await CreateUserAsync(userManager, demo.Email, demo.Password, demo.Role, demo.TenantId, logger);
            }
        }

        private static async Task CreateUserAsync(UserManager<AppUser> userManager, string email, string password,
            string role, int? tenantId, ILogger logger)
        {
            if (await userManager.FindByEmailAsync(email) != null) return;

            // A tenant can only have one login (unique index), so skip if someone already registered one
            if (tenantId.HasValue && await userManager.Users.AnyAsync(u => u.TenantId == tenantId))
            {
                logger.LogInformation("Tenant {TenantId} already has a login; skipping {Email}", tenantId, email);
                return;
            }

            var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, TenantId = tenantId };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Could not create account {Email}: {Errors}", email,
                    string.Join(" ", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(user, role);
            logger.LogInformation("Created {Role} account {Email}", role, email);
        }
    }
}
