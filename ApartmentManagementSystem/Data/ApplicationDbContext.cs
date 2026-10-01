using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Data
{
    // IdentityDbContext adds the ASP.NET Core Identity tables (users, roles, claims)
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Apartment> Apartments { get; set; }

        public DbSet<Tenant> Tenants { get; set; }

        public DbSet<Lease> Leases { get; set; }

        public DbSet<RentCharge> RentCharges { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Lease>(lease =>
            {
                lease.Property(l => l.MonthlyRent).HasPrecision(18, 2);
                lease.Property(l => l.SecurityDeposit).HasPrecision(18, 2);
                lease.Property(l => l.Status).HasConversion<string>();

                // Restrict deletes: lease history must not vanish when a tenant/apartment is removed.
                // (Also avoids SQL Server's "multiple cascade paths" error, since Tenant already cascades from Apartment.)
                lease.HasOne(l => l.Tenant)
                    .WithMany()
                    .HasForeignKey(l => l.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                lease.HasOne(l => l.Apartment)
                    .WithMany()
                    .HasForeignKey(l => l.ApartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RentCharge>(charge =>
            {
                charge.Property(c => c.Amount).HasPrecision(18, 2);
                charge.Property(c => c.AmountPaid).HasPrecision(18, 2);
                charge.Property(c => c.Status).HasConversion<string>();
                charge.Ignore(c => c.Balance); // calculated, not stored

                // One charge per lease per month, enforced by the database as well as the service
                charge.HasIndex(c => new { c.LeaseId, c.BillingMonth }).IsUnique();
                charge.HasIndex(c => new { c.Status, c.DueDate });

                // Billing history must not disappear with a lease
                charge.HasOne(c => c.Lease)
                    .WithMany()
                    .HasForeignKey(c => c.LeaseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payment>(payment =>
            {
                payment.Property(p => p.Amount).HasPrecision(18, 2);
                payment.Property(p => p.Method).HasConversion<string>();

                payment.HasOne(p => p.RentCharge)
                    .WithMany(c => c.Payments)
                    .HasForeignKey(p => p.RentChargeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AppUser>(user =>
            {
                // A tenant record can have at most one login
                user.HasIndex(u => u.TenantId).IsUnique().HasFilter("[TenantId] IS NOT NULL");

                user.HasOne(u => u.Tenant)
                    .WithMany()
                    .HasForeignKey(u => u.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RefreshToken>(token =>
            {
                token.Property(t => t.TokenHash).HasMaxLength(64);
                token.HasIndex(t => t.TokenHash).IsUnique();

                token.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MaintenanceRequest>(request =>
            {
                request.Property(r => r.Title).HasMaxLength(100);
                request.Property(r => r.Description).HasMaxLength(2000);
                request.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
                // Priority stays numeric so ORDER BY puts Urgent first

                request.HasIndex(r => new { r.Status, r.Priority });

                // Request history is kept even if a tenant/apartment record is removed
                request.HasOne(r => r.Apartment)
                    .WithMany()
                    .HasForeignKey(r => r.ApartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                request.HasOne(r => r.Tenant)
                    .WithMany()
                    .HasForeignKey(r => r.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            SeedData.Seed(modelBuilder);
        }
    }
}
