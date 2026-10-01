using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentManagementSystem.Tests
{
    public class BillingServiceTests
    {
        private static readonly DateTime Today = new DateTime(2026, 3, 15);

        private BillingService GetService(out ApplicationDbContext context)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
                .Options;

            context = new ApplicationDbContext(options);
            return new BillingService(context, new FixedTimeProvider(Today), TestCurrentUser.Admin);
        }

        // Lease from Jan 10, 2026 to Dec 31, 2026 at 1000/month
        private static async Task<Lease> SeedLeaseAsync(ApplicationDbContext context, LeaseStatus status = LeaseStatus.Active)
        {
            var apartment = new Apartment { Name = "Sunset Apartments", Location = "City Center", Address = "Zone 6", Rent = 1000, Rooms = 2 };
            context.Apartments.Add(apartment);
            await context.SaveChangesAsync();

            var tenant = new Tenant
            {
                Name = "Test tenant",
                Email = "Tenant@test.com",
                PhoneNumber = "123456789",
                PermanentAddress = "Permanent Address Sample",
                MoveInDate = new DateTime(2026, 1, 10),
                ApartmentId = apartment.Id
            };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();

            var lease = new Lease
            {
                TenantId = tenant.Id,
                ApartmentId = apartment.Id,
                StartDate = new DateTime(2026, 1, 10),
                EndDate = new DateTime(2026, 12, 31),
                MonthlyRent = 1000,
                SecurityDeposit = 2000,
                Status = status
            };
            context.Leases.Add(lease);
            await context.SaveChangesAsync();
            return lease;
        }

        private static Payment NewPayment(decimal amount) => new Payment
        {
            Amount = amount,
            PaidDate = new DateTime(2026, 3, 1),
            Method = PaymentMethod.BankTransfer,
            ReferenceNumber = "REF-001"
        };

        [Fact]
        public async Task GenerateChargesAsync_ShouldCreateOneChargePerMonthUpToAsOf()
        {
            var service = GetService(out var context);
            var lease = await SeedLeaseAsync(context);

            var created = await service.GenerateChargesAsync(Today);

            Assert.Equal(3, created); // Jan, Feb, Mar
            var charges = await context.RentCharges.OrderBy(c => c.BillingMonth).ToListAsync();
            Assert.Equal(new DateTime(2026, 1, 1), charges[0].BillingMonth);
            Assert.Equal(new DateTime(2026, 1, 5), charges[0].DueDate);
            Assert.All(charges, c => Assert.Equal(lease.MonthlyRent, c.Amount));
        }

        [Fact]
        public async Task GenerateChargesAsync_RunTwice_ShouldNotCreateDuplicates()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);

            await service.GenerateChargesAsync(Today);
            var secondRun = await service.GenerateChargesAsync(Today);

            Assert.Equal(0, secondRun);
            Assert.Equal(3, await context.RentCharges.CountAsync());
        }

        [Fact]
        public async Task GenerateChargesAsync_ShouldSkipInactiveLeases()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context, LeaseStatus.Terminated);

            var created = await service.GenerateChargesAsync(Today);

            Assert.Equal(0, created);
        }

        [Fact]
        public async Task GenerateChargesAsync_ShouldStopAtLeaseEndDate()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);

            var created = await service.GenerateChargesAsync(new DateTime(2027, 6, 1));

            Assert.Equal(12, created); // Jan-Dec 2026 only
        }

        [Fact]
        public async Task MarkOverdueAsync_ShouldOnlyMarkChargesPastDueDate()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);
            await service.GenerateChargesAsync(Today);

            var marked = await service.MarkOverdueAsync(Today);

            // Jan 5, Feb 5 and Mar 5 are all before Mar 15
            Assert.Equal(3, marked);

            // On Mar 3 only Jan and Feb are late
            var service2 = GetService(out var context2);
            await SeedLeaseAsync(context2);
            await service2.GenerateChargesAsync(new DateTime(2026, 3, 3));
            Assert.Equal(2, await service2.MarkOverdueAsync(new DateTime(2026, 3, 3)));
        }

        [Fact]
        public async Task RecordPaymentAsync_FullPayment_ShouldMarkChargePaid()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);
            await service.GenerateChargesAsync(Today);
            var charge = await context.RentCharges.FirstAsync();

            var payment = await service.RecordPaymentAsync(charge.Id, NewPayment(1000));

            Assert.Equal(RentChargeStatus.Paid, payment.RentCharge.Status);
            Assert.Equal(0, payment.RentCharge.Balance);
        }

        [Fact]
        public async Task RecordPaymentAsync_PartialPayments_ShouldAccumulate()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);
            await service.GenerateChargesAsync(Today);
            await service.MarkOverdueAsync(Today);
            var charge = await context.RentCharges.FirstAsync();

            var first = await service.RecordPaymentAsync(charge.Id, NewPayment(400));
            Assert.Equal(RentChargeStatus.Overdue, first.RentCharge.Status);
            Assert.Equal(600, first.RentCharge.Balance);

            var second = await service.RecordPaymentAsync(charge.Id, NewPayment(600));
            Assert.Equal(RentChargeStatus.Paid, second.RentCharge.Status);
        }

        [Fact]
        public async Task RecordPaymentAsync_ShouldThrow_WhenAmountExceedsBalance()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);
            await service.GenerateChargesAsync(Today);
            var charge = await context.RentCharges.FirstAsync();

            await Assert.ThrowsAsync<ArgumentException>(() => service.RecordPaymentAsync(charge.Id, NewPayment(1500)));
        }

        [Fact]
        public async Task RecordPaymentAsync_ShouldThrow_WhenAmountNotPositive()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);
            await service.GenerateChargesAsync(Today);
            var charge = await context.RentCharges.FirstAsync();

            await Assert.ThrowsAsync<ArgumentException>(() => service.RecordPaymentAsync(charge.Id, NewPayment(0)));
        }

        [Fact]
        public async Task RecordPaymentAsync_ShouldThrow_WhenPaidDateInFuture()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);
            await service.GenerateChargesAsync(Today);
            var charge = await context.RentCharges.FirstAsync();
            var payment = NewPayment(500);
            payment.PaidDate = Today.AddDays(1);

            await Assert.ThrowsAsync<ArgumentException>(() => service.RecordPaymentAsync(charge.Id, payment));
        }

        [Fact]
        public async Task RecordPaymentAsync_ShouldThrow_WhenChargeAlreadyPaid()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);
            await service.GenerateChargesAsync(Today);
            var charge = await context.RentCharges.FirstAsync();
            await service.RecordPaymentAsync(charge.Id, NewPayment(1000));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordPaymentAsync(charge.Id, NewPayment(100)));
        }

        [Fact]
        public async Task RecordPaymentAsync_ShouldThrow_WhenChargeNotFound()
        {
            var service = GetService(out _);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RecordPaymentAsync(999, NewPayment(100)));
        }

        [Fact]
        public async Task GetLeaseBalanceAsync_ShouldSumChargesPaymentsAndOverdue()
        {
            var service = GetService(out var context);
            var lease = await SeedLeaseAsync(context);
            await service.RunDailyBillingAsync(Today);           // 3 charges, all overdue
            var jan = await context.RentCharges.OrderBy(c => c.BillingMonth).FirstAsync();
            await service.RecordPaymentAsync(jan.Id, NewPayment(1000)); // pay January in full

            var balance = await service.GetLeaseBalanceAsync(lease.Id);

            Assert.Equal(3000, balance.TotalCharged);
            Assert.Equal(1000, balance.TotalPaid);
            Assert.Equal(2000, balance.Balance);
            Assert.Equal(2000, balance.OverdueAmount);
            Assert.Equal(2, balance.OverdueCount);
        }

        [Fact]
        public async Task GetOverdueChargesAsync_ShouldReturnOldestFirst()
        {
            var service = GetService(out var context);
            await SeedLeaseAsync(context);
            await service.RunDailyBillingAsync(Today);

            var overdue = await service.GetOverdueChargesAsync(new PaginationQuery());

            Assert.Equal(3, overdue.TotalCount);
            Assert.Equal(new DateTime(2026, 1, 5), overdue.Items.First().DueDate);
            Assert.Equal("Test tenant", overdue.Items.First().Lease.Tenant.Name);
        }
    }
}
