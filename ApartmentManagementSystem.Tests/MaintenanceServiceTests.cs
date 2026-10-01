using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using ApartmentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentManagementSystem.Tests
{
    public class MaintenanceServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc);

        private class Seeded
        {
            public ApplicationDbContext Context { get; init; }
            public Lease LeaseA { get; init; }   // tenant A, active lease
            public Lease LeaseB { get; init; }   // tenant B, active lease
            public Apartment VacantApartment { get; init; }
            public Tenant TenantWithoutLease { get; init; }
        }

        private static async Task<Seeded> SeedAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
                .Options;
            var context = new ApplicationDbContext(options);

            Apartment NewApartment(string name) => new Apartment { Name = name, Location = "Cebu", Address = "Zone 6", Rent = 1000, Rooms = 1 };
            Tenant NewTenant(string name, Apartment apt) => new Tenant
            {
                Name = name, Email = $"{name}@test.com", PhoneNumber = "123456789",
                PermanentAddress = "Address", MoveInDate = new DateTime(2026, 1, 1), Apartment = apt
            };
            Lease NewLease(Tenant t, Apartment a, LeaseStatus status = LeaseStatus.Active) => new Lease
            {
                Tenant = t, Apartment = a, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31),
                MonthlyRent = 1000, SecurityDeposit = 2000, Status = status
            };

            var aptA = NewApartment("A");
            var aptB = NewApartment("B");
            var vacant = NewApartment("Vacant");
            var leaseA = NewLease(NewTenant("TenantA", aptA), aptA);
            var leaseB = NewLease(NewTenant("TenantB", aptB), aptB);
            var formerTenant = NewTenant("Former", vacant);
            context.Leases.AddRange(leaseA, leaseB, NewLease(formerTenant, vacant, LeaseStatus.Terminated));
            await context.SaveChangesAsync();

            return new Seeded { Context = context, LeaseA = leaseA, LeaseB = leaseB, VacantApartment = vacant, TenantWithoutLease = formerTenant };
        }

        private static MaintenanceService Service(Seeded s, ICurrentUser user) =>
            new MaintenanceService(s.Context, user, new FixedTimeProvider(Now));

        private static MaintenanceCreateDto NewRequest(MaintenancePriority priority = MaintenancePriority.Normal, int? apartmentId = null) =>
            new MaintenanceCreateDto { ApartmentId = apartmentId, Title = "Leaking faucet", Description = "Drips all day", Priority = priority };

        // --- Creating ---

        [Fact]
        public async Task Tenant_Create_ShouldUseApartmentFromActiveLease()
        {
            var s = await SeedAsync();
            var service = Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId));

            var request = await service.CreateAsync(NewRequest());

            Assert.Equal(s.LeaseA.ApartmentId, request.ApartmentId);
            Assert.Equal(s.LeaseA.TenantId, request.TenantId);
            Assert.Equal(MaintenanceStatus.Open, request.Status);
            Assert.Equal(Now, request.CreatedAt);
        }

        [Fact]
        public async Task Tenant_Create_ForAnotherApartment_ShouldThrow()
        {
            var s = await SeedAsync();
            var service = Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.CreateAsync(NewRequest(apartmentId: s.LeaseB.ApartmentId)));
        }

        [Fact]
        public async Task Tenant_Create_WithoutActiveLease_ShouldThrow()
        {
            var s = await SeedAsync();
            var service = Service(s, TestCurrentUser.ForTenant(s.TenantWithoutLease.Id));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(NewRequest()));
        }

        [Fact]
        public async Task Admin_Create_ForVacantApartment_ShouldHaveNoTenant()
        {
            var s = await SeedAsync();
            var service = Service(s, TestCurrentUser.Admin);

            var request = await service.CreateAsync(NewRequest(apartmentId: s.VacantApartment.Id));

            Assert.Equal(s.VacantApartment.Id, request.ApartmentId);
            Assert.Null(request.TenantId);
        }

        [Fact]
        public async Task Admin_Create_WithoutApartment_ShouldThrow()
        {
            var s = await SeedAsync();
            var service = Service(s, TestCurrentUser.Admin);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(NewRequest()));
        }

        [Theory]
        [InlineData("", "Description")]
        [InlineData("Title", "")]
        [InlineData("   ", "Description")]
        public async Task Create_WithMissingText_ShouldThrow(string title, string description)
        {
            var s = await SeedAsync();
            var service = Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId));

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateAsync(new MaintenanceCreateDto { Title = title, Description = description }));
        }

        [Fact]
        public async Task Create_WithTooLongTitle_ShouldThrow()
        {
            var s = await SeedAsync();
            var service = Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId));

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateAsync(new MaintenanceCreateDto { Title = new string('x', 101), Description = "d" }));
        }

        // --- Status workflow ---

        [Theory]
        [InlineData(MaintenanceStatus.Open, MaintenanceStatus.InProgress, true)]
        [InlineData(MaintenanceStatus.Open, MaintenanceStatus.Cancelled, true)]
        [InlineData(MaintenanceStatus.InProgress, MaintenanceStatus.Resolved, true)]
        [InlineData(MaintenanceStatus.InProgress, MaintenanceStatus.Open, true)]
        [InlineData(MaintenanceStatus.Open, MaintenanceStatus.Resolved, false)]      // must be worked on first
        [InlineData(MaintenanceStatus.Resolved, MaintenanceStatus.Open, false)]      // final
        [InlineData(MaintenanceStatus.Cancelled, MaintenanceStatus.InProgress, false)] // final
        [InlineData(MaintenanceStatus.InProgress, MaintenanceStatus.Cancelled, false)] // work already started
        public void CanMove_ShouldFollowWorkflow(MaintenanceStatus from, MaintenanceStatus to, bool expected)
        {
            Assert.Equal(expected, MaintenanceRequest.CanMove(from, to));
        }

        [Fact]
        public async Task Admin_FullWorkflow_ShouldResolveWithNotes()
        {
            var s = await SeedAsync();
            var created = await Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId)).CreateAsync(NewRequest());
            var admin = Service(s, TestCurrentUser.Admin);

            await admin.UpdateStatusAsync(created.Id, MaintenanceStatus.InProgress, null);
            var resolved = await admin.UpdateStatusAsync(created.Id, MaintenanceStatus.Resolved, "Replaced washer");

            Assert.Equal(MaintenanceStatus.Resolved, resolved.Status);
            Assert.Equal("Replaced washer", resolved.ResolutionNotes);
            Assert.Equal(Now, resolved.ResolvedAt);
        }

        [Fact]
        public async Task UpdateStatus_InvalidTransition_ShouldThrow()
        {
            var s = await SeedAsync();
            var admin = Service(s, TestCurrentUser.Admin);
            var created = await admin.CreateAsync(NewRequest(apartmentId: s.VacantApartment.Id));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => admin.UpdateStatusAsync(created.Id, MaintenanceStatus.Resolved, "skipped straight to resolved"));
        }

        [Fact]
        public async Task UpdateStatus_ResolveWithoutNotes_ShouldThrow()
        {
            var s = await SeedAsync();
            var admin = Service(s, TestCurrentUser.Admin);
            var created = await admin.CreateAsync(NewRequest(apartmentId: s.VacantApartment.Id));
            await admin.UpdateStatusAsync(created.Id, MaintenanceStatus.InProgress, null);

            await Assert.ThrowsAsync<ArgumentException>(
                () => admin.UpdateStatusAsync(created.Id, MaintenanceStatus.Resolved, " "));
        }

        [Fact]
        public async Task UpdateStatus_NotFound_ShouldThrow()
        {
            var s = await SeedAsync();

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => Service(s, TestCurrentUser.Admin).UpdateStatusAsync(999, MaintenanceStatus.InProgress, null));
        }

        // --- Cancelling ---

        [Fact]
        public async Task Tenant_CanCancelOwnOpenRequest()
        {
            var s = await SeedAsync();
            var tenant = Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId));
            var created = await tenant.CreateAsync(NewRequest());

            var cancelled = await tenant.CancelAsync(created.Id);

            Assert.Equal(MaintenanceStatus.Cancelled, cancelled.Status);
        }

        [Fact]
        public async Task Tenant_CannotCancelAfterWorkStarted()
        {
            var s = await SeedAsync();
            var tenant = Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId));
            var created = await tenant.CreateAsync(NewRequest());
            await Service(s, TestCurrentUser.Admin).UpdateStatusAsync(created.Id, MaintenanceStatus.InProgress, null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => tenant.CancelAsync(created.Id));
        }

        [Fact]
        public async Task Tenant_CannotCancelAnotherTenantsRequest()
        {
            var s = await SeedAsync();
            var created = await Service(s, TestCurrentUser.ForTenant(s.LeaseB.TenantId)).CreateAsync(NewRequest());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId)).CancelAsync(created.Id));
        }

        // --- Reading ---

        [Fact]
        public async Task Tenant_CannotReadAnotherTenantsRequest()
        {
            var s = await SeedAsync();
            var created = await Service(s, TestCurrentUser.ForTenant(s.LeaseB.TenantId)).CreateAsync(NewRequest());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId)).GetByIdAsync(created.Id));
        }

        [Fact]
        public async Task Tenant_CannotReadAdminLoggedRequest()
        {
            var s = await SeedAsync();
            var created = await Service(s, TestCurrentUser.Admin).CreateAsync(NewRequest(apartmentId: s.VacantApartment.Id));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId)).GetByIdAsync(created.Id));
        }

        [Fact]
        public async Task GetForTenant_ShouldOnlyReturnOwnRequests()
        {
            var s = await SeedAsync();
            var tenantA = Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId));
            await tenantA.CreateAsync(NewRequest());
            await tenantA.CreateAsync(NewRequest());
            await Service(s, TestCurrentUser.ForTenant(s.LeaseB.TenantId)).CreateAsync(NewRequest());

            var mine = await tenantA.GetForTenantAsync(s.LeaseA.TenantId, new MaintenanceQuery());

            Assert.Equal(2, mine.TotalCount);
            Assert.All(mine.Items, r => Assert.Equal(s.LeaseA.TenantId, r.TenantId));
        }

        [Fact]
        public async Task Admin_GetAll_ShouldSortUrgentFirstThenOldest()
        {
            var s = await SeedAsync();
            var admin = Service(s, TestCurrentUser.Admin);
            var low = await admin.CreateAsync(NewRequest(MaintenancePriority.Low, s.VacantApartment.Id));
            var urgent = await admin.CreateAsync(NewRequest(MaintenancePriority.Urgent, s.VacantApartment.Id));
            var high = await admin.CreateAsync(NewRequest(MaintenancePriority.High, s.VacantApartment.Id));

            var all = await admin.GetAllAsync(new MaintenanceQuery());

            Assert.Equal(new[] { urgent.Id, high.Id, low.Id }, all.Items.Select(r => r.Id));
        }

        [Fact]
        public async Task Admin_GetAll_ShouldFilterByStatusAndPriority()
        {
            var s = await SeedAsync();
            var admin = Service(s, TestCurrentUser.Admin);
            var urgent = await admin.CreateAsync(NewRequest(MaintenancePriority.Urgent, s.VacantApartment.Id));
            await admin.CreateAsync(NewRequest(MaintenancePriority.Low, s.VacantApartment.Id));
            var inProgress = await admin.CreateAsync(NewRequest(MaintenancePriority.Urgent, s.VacantApartment.Id));
            await admin.UpdateStatusAsync(inProgress.Id, MaintenanceStatus.InProgress, null);

            var result = await admin.GetAllAsync(new MaintenanceQuery { Status = MaintenanceStatus.Open, Priority = MaintenancePriority.Urgent });

            Assert.Equal(new[] { urgent.Id }, result.Items.Select(r => r.Id));
        }
    }
}
