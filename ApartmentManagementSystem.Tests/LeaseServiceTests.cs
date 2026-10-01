using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentManagementSystem.Tests
{
    public class LeaseServiceTests
    {
        private LeaseService GetService(out ApplicationDbContext context)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
                .Options;

            context = new ApplicationDbContext(options);
            return new LeaseService(context, TestCurrentUser.Admin);
        }

        private static async Task<(Tenant tenant, Apartment apartment)> SeedAsync(ApplicationDbContext context)
        {
            var apartment = new Apartment
            {
                Name = "Sunset Apartments",
                Location = "City Center",
                Address = "Zone 6",
                Rent = 1000,
                Rooms = 2,
                HasParking = true
            };
            context.Apartments.Add(apartment);
            await context.SaveChangesAsync();

            var tenant = new Tenant
            {
                Name = "Test tenant",
                Email = "Tenant@test.com",
                PhoneNumber = "123456789",
                PermanentAddress = "Permanent Address Sample",
                MoveInDate = DateTime.UtcNow,
                ApartmentId = apartment.Id
            };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();

            return (tenant, apartment);
        }

        private static Lease NewLease(int tenantId, int apartmentId) => new Lease
        {
            TenantId = tenantId,
            ApartmentId = apartmentId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            MonthlyRent = 1000,
            SecurityDeposit = 2000
        };

        [Fact]
        public async Task CreateAsync_ShouldCreateActiveLease()
        {
            var service = GetService(out var context);
            var (tenant, apartment) = await SeedAsync(context);

            var lease = await service.CreateAsync(NewLease(tenant.Id, apartment.Id));

            Assert.Equal(LeaseStatus.Active, lease.Status);
            Assert.Equal("Test tenant", lease.Tenant.Name);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenApartmentAlreadyHasActiveLease()
        {
            var service = GetService(out var context);
            var (tenant, apartment) = await SeedAsync(context);
            await service.CreateAsync(NewLease(tenant.Id, apartment.Id));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateAsync(NewLease(tenant.Id, apartment.Id)));
        }

        [Fact]
        public async Task CreateAsync_ShouldAllowNewLease_AfterPreviousTerminated()
        {
            var service = GetService(out var context);
            var (tenant, apartment) = await SeedAsync(context);
            var first = await service.CreateAsync(NewLease(tenant.Id, apartment.Id));
            await service.TerminateAsync(first.Id, new DateTime(2026, 6, 1));

            var second = await service.CreateAsync(NewLease(tenant.Id, apartment.Id));

            Assert.Equal(LeaseStatus.Active, second.Status);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenEndDateBeforeStartDate()
        {
            var service = GetService(out var context);
            var (tenant, apartment) = await SeedAsync(context);
            var lease = NewLease(tenant.Id, apartment.Id);
            lease.EndDate = lease.StartDate.AddDays(-1);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(lease));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenRentNotPositive()
        {
            var service = GetService(out var context);
            var (tenant, apartment) = await SeedAsync(context);
            var lease = NewLease(tenant.Id, apartment.Id);
            lease.MonthlyRent = 0;

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(lease));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenTenantDoesNotExist()
        {
            var service = GetService(out var context);
            var (_, apartment) = await SeedAsync(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.CreateAsync(NewLease(999, apartment.Id)));
        }

        [Fact]
        public async Task TerminateAsync_ShouldSetStatusAndDate()
        {
            var service = GetService(out var context);
            var (tenant, apartment) = await SeedAsync(context);
            var lease = await service.CreateAsync(NewLease(tenant.Id, apartment.Id));

            var terminated = await service.TerminateAsync(lease.Id, new DateTime(2026, 6, 1));

            Assert.Equal(LeaseStatus.Terminated, terminated.Status);
            Assert.Equal(new DateTime(2026, 6, 1), terminated.TerminatedDate);
        }

        [Fact]
        public async Task TerminateAsync_ShouldThrow_WhenAlreadyTerminated()
        {
            var service = GetService(out var context);
            var (tenant, apartment) = await SeedAsync(context);
            var lease = await service.CreateAsync(NewLease(tenant.Id, apartment.Id));
            await service.TerminateAsync(lease.Id, new DateTime(2026, 6, 1));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.TerminateAsync(lease.Id, new DateTime(2026, 7, 1)));
        }

        [Fact]
        public async Task TerminateAsync_ShouldThrow_WhenDateOutsideLeasePeriod()
        {
            var service = GetService(out var context);
            var (tenant, apartment) = await SeedAsync(context);
            var lease = await service.CreateAsync(NewLease(tenant.Id, apartment.Id));

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.TerminateAsync(lease.Id, new DateTime(2027, 6, 1)));
        }

        [Fact]
        public async Task GetAvailableAsync_ShouldExcludeApartmentsWithActiveLease()
        {
            var service = GetService(out var context);
            var (tenant, apartment) = await SeedAsync(context);
            var apartmentService = new ApartmentService(context);

            await service.CreateAsync(NewLease(tenant.Id, apartment.Id));

            var available = await apartmentService.GetAvailableAsync(new PaginationQuery());
            Assert.DoesNotContain(available.Items, a => a.Id == apartment.Id);
        }
    }
}
