using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using ApartmentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentManagementSystem.Tests
{
    // Tenants may only see their own leases and billing; admins see everything
    public class AuthorizationTests
    {
        private static readonly DateTime Today = new DateTime(2026, 3, 15);

        private class Seeded
        {
            public ApplicationDbContext Context { get; init; }
            public Lease LeaseA { get; init; }
            public Lease LeaseB { get; init; }
        }

        // Two tenants, each with their own apartment and lease
        private static async Task<Seeded> SeedAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
                .Options;
            var context = new ApplicationDbContext(options);

            Lease CreateLease(string name)
            {
                var apartment = new Apartment { Name = $"{name} Apt", Location = "Cebu", Address = "Zone 6", Rent = 1000, Rooms = 1 };
                var tenant = new Tenant
                {
                    Name = name,
                    Email = $"{name}@test.com",
                    PhoneNumber = "123456789",
                    PermanentAddress = "Address",
                    MoveInDate = new DateTime(2026, 1, 1),
                    Apartment = apartment
                };
                var lease = new Lease
                {
                    Tenant = tenant,
                    Apartment = apartment,
                    StartDate = new DateTime(2026, 1, 1),
                    EndDate = new DateTime(2026, 12, 31),
                    MonthlyRent = 1000,
                    SecurityDeposit = 2000
                };
                context.Leases.Add(lease);
                return lease;
            }

            var leaseA = CreateLease("TenantA");
            var leaseB = CreateLease("TenantB");
            await context.SaveChangesAsync();

            return new Seeded { Context = context, LeaseA = leaseA, LeaseB = leaseB };
        }

        [Fact]
        public async Task Tenant_CanReadOwnLease()
        {
            var s = await SeedAsync();
            var service = new LeaseService(s.Context, TestCurrentUser.ForTenant(s.LeaseA.TenantId));

            var lease = await service.GetByIdAsync(s.LeaseA.Id);

            Assert.NotNull(lease);
        }

        [Fact]
        public async Task Tenant_CannotReadAnotherTenantsLease()
        {
            var s = await SeedAsync();
            var service = new LeaseService(s.Context, TestCurrentUser.ForTenant(s.LeaseA.TenantId));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetByIdAsync(s.LeaseB.Id));
        }

        [Fact]
        public async Task Tenant_CannotListAnotherTenantsLeases()
        {
            var s = await SeedAsync();
            var service = new LeaseService(s.Context, TestCurrentUser.ForTenant(s.LeaseA.TenantId));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.GetForTenantAsync(s.LeaseB.TenantId, new PaginationQuery()));
        }

        [Fact]
        public async Task Tenant_CannotSeeAnotherTenantsBalanceOrCharges()
        {
            var s = await SeedAsync();
            var service = new BillingService(s.Context, new FixedTimeProvider(Today), TestCurrentUser.ForTenant(s.LeaseA.TenantId));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetLeaseBalanceAsync(s.LeaseB.Id));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.GetChargesForLeaseAsync(s.LeaseB.Id, new PaginationQuery()));
        }

        [Fact]
        public async Task Tenant_CanSeeOwnBalance()
        {
            var s = await SeedAsync();
            var service = new BillingService(s.Context, new FixedTimeProvider(Today), TestCurrentUser.ForTenant(s.LeaseA.TenantId));

            var balance = await service.GetLeaseBalanceAsync(s.LeaseA.Id);

            Assert.Equal(s.LeaseA.Id, balance.LeaseId);
        }

        [Fact]
        public async Task Admin_CanReadAnyLeaseAndBalance()
        {
            var s = await SeedAsync();
            var leases = new LeaseService(s.Context, TestCurrentUser.Admin);
            var billing = new BillingService(s.Context, new FixedTimeProvider(Today), TestCurrentUser.Admin);

            Assert.NotNull(await leases.GetByIdAsync(s.LeaseA.Id));
            Assert.NotNull(await leases.GetByIdAsync(s.LeaseB.Id));
            Assert.Equal(s.LeaseB.Id, (await billing.GetLeaseBalanceAsync(s.LeaseB.Id)).LeaseId);
        }

        [Fact]
        public async Task UserWithoutTenantOrAdminRole_CannotReadLeases()
        {
            var s = await SeedAsync();
            var service = new LeaseService(s.Context, new TestCurrentUser());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetByIdAsync(s.LeaseA.Id));
        }

        [Fact]
        public async Task BackgroundBilling_RunsWithoutAUser()
        {
            var s = await SeedAsync();
            // The background job has no logged-in user
            var service = new BillingService(s.Context, new FixedTimeProvider(Today), new TestCurrentUser());

            var result = await service.RunDailyBillingAsync(Today);

            Assert.Equal(6, result.ChargesCreated); // 2 leases x Jan-Mar
        }
    }
}
