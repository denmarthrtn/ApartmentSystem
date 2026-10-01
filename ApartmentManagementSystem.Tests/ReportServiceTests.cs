using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using ApartmentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentManagementSystem.Tests
{
    public class ReportServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime March = new DateTime(2026, 3, 1);
        private static readonly DateTime February = new DateTime(2026, 2, 1);

        private class Seeded
        {
            public ApplicationDbContext Context { get; init; }
            public Lease LeaseA { get; init; }
            public Lease LeaseB { get; init; }
            public Apartment VacantApartment { get; init; }
        }

        // 4 apartments: A and B leased, C's lease was terminated, D never leased
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
            var aptC = NewApartment("C");
            var aptD = NewApartment("D");
            var leaseA = NewLease(NewTenant("TenantA", aptA), aptA);
            var leaseB = NewLease(NewTenant("TenantB", aptB), aptB);
            context.Apartments.Add(aptD);
            context.Leases.AddRange(leaseA, leaseB, NewLease(NewTenant("Former", aptC), aptC, LeaseStatus.Terminated));
            await context.SaveChangesAsync();

            return new Seeded { Context = context, LeaseA = leaseA, LeaseB = leaseB, VacantApartment = aptC };
        }

        private static ReportService Service(Seeded s, ICurrentUser user) =>
            new ReportService(s.Context, user, new FixedTimeProvider(Now));

        private static RentCharge Charge(Lease lease, DateTime month, decimal paid, RentChargeStatus status) => new RentCharge
        {
            LeaseId = lease.Id, BillingMonth = month, DueDate = month.AddDays(4),
            Amount = 1000, AmountPaid = paid, Status = status
        };

        private static MaintenanceRequest Request(Apartment apt, MaintenanceStatus status, MaintenancePriority priority = MaintenancePriority.Normal) => new MaintenanceRequest
        {
            ApartmentId = apt.Id, Title = "Fix", Description = "Broken",
            Status = status, Priority = priority, CreatedAt = Now, UpdatedAt = Now
        };

        [Fact]
        public async Task Summary_ShouldCountOccupancyFromActiveLeases()
        {
            var s = await SeedAsync();

            var summary = await Service(s, TestCurrentUser.Admin).GetSummaryAsync();

            Assert.Equal(4, summary.TotalApartments);
            Assert.Equal(2, summary.OccupiedApartments);   // terminated lease doesn't count
            Assert.Equal(2, summary.VacantApartments);
            Assert.Equal(50m, summary.OccupancyRate);
        }

        [Fact]
        public async Task Summary_WithNoApartments_ShouldReturnZeroOccupancy()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var service = new ReportService(new ApplicationDbContext(options), TestCurrentUser.Admin, new FixedTimeProvider(Now));

            var summary = await service.GetSummaryAsync();

            Assert.Equal(0, summary.TotalApartments);
            Assert.Equal(0m, summary.OccupancyRate);
            Assert.Equal(0m, summary.TotalOverdue);
        }

        [Fact]
        public async Task Summary_ShouldOnlyIncludeThisMonthsBillingAndPayments()
        {
            var s = await SeedAsync();
            var februaryCharge = Charge(s.LeaseA, February, 1000, RentChargeStatus.Paid);
            var marchCharge = Charge(s.LeaseA, March, 600, RentChargeStatus.Pending);
            s.Context.RentCharges.AddRange(februaryCharge, marchCharge, Charge(s.LeaseB, March, 0, RentChargeStatus.Pending));
            s.Context.Payments.AddRange(
                new Payment { RentCharge = februaryCharge, Amount = 1000, PaidDate = new DateTime(2026, 2, 28), Method = PaymentMethod.Cash },
                new Payment { RentCharge = marchCharge, Amount = 600, PaidDate = new DateTime(2026, 3, 3), Method = PaymentMethod.Cash });
            await s.Context.SaveChangesAsync();

            var summary = await Service(s, TestCurrentUser.Admin).GetSummaryAsync();

            Assert.Equal(March, summary.Month);
            Assert.Equal(2000m, summary.RentBilledThisMonth);
            Assert.Equal(600m, summary.RentCollectedThisMonth);
        }

        [Fact]
        public async Task Summary_ShouldCountLatePaymentOfOldChargeAsCollectedThisMonth()
        {
            var s = await SeedAsync();
            var februaryCharge = Charge(s.LeaseA, February, 1000, RentChargeStatus.Paid);
            s.Context.RentCharges.Add(februaryCharge);
            s.Context.Payments.Add(new Payment { RentCharge = februaryCharge, Amount = 1000, PaidDate = new DateTime(2026, 3, 10), Method = PaymentMethod.BankTransfer });
            await s.Context.SaveChangesAsync();

            var summary = await Service(s, TestCurrentUser.Admin).GetSummaryAsync();

            Assert.Equal(0m, summary.RentBilledThisMonth);
            Assert.Equal(1000m, summary.RentCollectedThisMonth);
        }

        [Fact]
        public async Task Summary_TotalOverdue_ShouldBeRemainingBalanceOfOverdueCharges()
        {
            var s = await SeedAsync();
            s.Context.RentCharges.AddRange(
                Charge(s.LeaseA, February, 0, RentChargeStatus.Overdue),     // owes 1000
                Charge(s.LeaseB, February, 250, RentChargeStatus.Overdue),   // owes 750
                Charge(s.LeaseA, March, 0, RentChargeStatus.Pending));       // not overdue yet
            await s.Context.SaveChangesAsync();

            var summary = await Service(s, TestCurrentUser.Admin).GetSummaryAsync();

            Assert.Equal(2, summary.OverdueCharges);
            Assert.Equal(1750m, summary.TotalOverdue);
        }

        [Fact]
        public async Task Summary_ShouldCountOnlyUnfinishedMaintenanceRequests()
        {
            var s = await SeedAsync();
            var apt = s.VacantApartment;
            s.Context.MaintenanceRequests.AddRange(
                Request(apt, MaintenanceStatus.Open),
                Request(apt, MaintenanceStatus.Open, MaintenancePriority.Urgent),
                Request(apt, MaintenanceStatus.InProgress, MaintenancePriority.Urgent),
                Request(apt, MaintenanceStatus.Resolved, MaintenancePriority.Urgent),
                Request(apt, MaintenanceStatus.Cancelled));
            await s.Context.SaveChangesAsync();

            var summary = await Service(s, TestCurrentUser.Admin).GetSummaryAsync();

            Assert.Equal(2, summary.OpenMaintenanceRequests);
            Assert.Equal(1, summary.InProgressMaintenanceRequests);
            Assert.Equal(2, summary.UrgentMaintenanceRequests);   // resolved urgent one is excluded
        }

        [Fact]
        public async Task Summary_AsTenant_ShouldThrowUnauthorized()
        {
            var s = await SeedAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                Service(s, TestCurrentUser.ForTenant(s.LeaseA.TenantId)).GetSummaryAsync());
        }
    }
}
