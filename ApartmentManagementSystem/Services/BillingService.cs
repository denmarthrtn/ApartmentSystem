using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Extensions;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    public class BillingService : IBillingService
    {
        // Rent is due on the 5th of each month
        public const int DueDay = 5;

        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _timeProvider;
        private readonly ICurrentUser _currentUser;

        public BillingService(ApplicationDbContext context, TimeProvider timeProvider, ICurrentUser currentUser)
        {
            _context = context;
            _timeProvider = timeProvider;
            _currentUser = currentUser;
        }

        // Creates any missing monthly charges for active leases, from the lease start month up to asOf.
        // Safe to run repeatedly: months that already have a charge are skipped, so a missed day catches up.
        public async Task<int> GenerateChargesAsync(DateTime asOf)
        {
            var activeLeases = await _context.Leases
                .Where(l => l.Status == LeaseStatus.Active)
                .ToListAsync();

            var leaseIds = activeLeases.Select(l => l.Id).ToList();
            var existingMonths = (await _context.RentCharges
                    .Where(c => leaseIds.Contains(c.LeaseId))
                    .Select(c => new { c.LeaseId, c.BillingMonth })
                    .ToListAsync())
                .Select(c => (c.LeaseId, c.BillingMonth))
                .ToHashSet();

            var created = 0;
            foreach (var lease in activeLeases)
            {
                var lastDate = asOf.Date < lease.EndDate ? asOf.Date : lease.EndDate;
                var month = FirstOfMonth(lease.StartDate);

                while (month <= lastDate)
                {
                    if (!existingMonths.Contains((lease.Id, month)))
                    {
                        _context.RentCharges.Add(new RentCharge
                        {
                            LeaseId = lease.Id,
                            BillingMonth = month,
                            DueDate = month.AddDays(DueDay - 1),
                            Amount = lease.MonthlyRent,
                            Status = RentChargeStatus.Pending
                        });
                        created++;
                    }
                    month = month.AddMonths(1);
                }
            }

            await _context.SaveChangesAsync();
            return created;
        }

        // Unpaid charges whose due date has passed become Overdue
        public async Task<int> MarkOverdueAsync(DateTime asOf)
        {
            var lateCharges = await _context.RentCharges
                .Where(c => c.Status == RentChargeStatus.Pending && c.DueDate < asOf.Date)
                .ToListAsync();

            foreach (var charge in lateCharges)
                charge.Status = RentChargeStatus.Overdue;

            await _context.SaveChangesAsync();
            return lateCharges.Count;
        }

        public async Task<BillingRunResultDto> RunDailyBillingAsync(DateTime asOf)
        {
            var created = await GenerateChargesAsync(asOf);
            var overdue = await MarkOverdueAsync(asOf);
            return new BillingRunResultDto { AsOf = asOf.Date, ChargesCreated = created, ChargesMarkedOverdue = overdue };
        }

        public async Task<Payment> RecordPaymentAsync(int rentChargeId, Payment payment)
        {
            var charge = await _context.RentCharges.FirstOrDefaultAsync(c => c.Id == rentChargeId)
                ?? throw new KeyNotFoundException($"Rent charge {rentChargeId} not found");

            if (charge.Status == RentChargeStatus.Paid)
                throw new InvalidOperationException("This charge is already fully paid");
            if (payment.Amount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero");
            if (payment.Amount > charge.Balance)
                throw new ArgumentException($"Payment of {payment.Amount} exceeds the remaining balance of {charge.Balance}");
            if (payment.PaidDate.Date > _timeProvider.GetLocalNow().Date)
                throw new ArgumentException("Paid date cannot be in the future");

            payment.RentChargeId = charge.Id;
            charge.AmountPaid += payment.Amount;
            if (charge.Balance == 0)
                charge.Status = RentChargeStatus.Paid;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            payment.RentCharge = charge;
            return payment;
        }

        public async Task<PagedResult<RentCharge>> GetChargesForLeaseAsync(int leaseId, PaginationQuery pagination)
        {
            await EnsureCanAccessLeaseAsync(leaseId);

            return await ChargesWithDetails()
                .Where(c => c.LeaseId == leaseId)
                .OrderByDescending(c => c.BillingMonth)
                .ToPagedResultAsync(pagination);
        }

        public async Task<PagedResult<RentCharge>> GetOverdueChargesAsync(PaginationQuery pagination)
        {
            return await ChargesWithDetails()
                .Where(c => c.Status == RentChargeStatus.Overdue)
                .OrderBy(c => c.DueDate)
                .ThenBy(c => c.Id)
                .ToPagedResultAsync(pagination);
        }

        public async Task<LeaseBalanceDto> GetLeaseBalanceAsync(int leaseId)
        {
            await EnsureCanAccessLeaseAsync(leaseId);

            var charges = await _context.RentCharges
                .AsNoTracking()
                .Where(c => c.LeaseId == leaseId)
                .ToListAsync();

            var overdue = charges.Where(c => c.Status == RentChargeStatus.Overdue).ToList();
            return new LeaseBalanceDto
            {
                LeaseId = leaseId,
                TotalCharged = charges.Sum(c => c.Amount),
                TotalPaid = charges.Sum(c => c.AmountPaid),
                Balance = charges.Sum(c => c.Balance),
                OverdueAmount = overdue.Sum(c => c.Balance),
                OverdueCount = overdue.Count
            };
        }

        // Lease must exist, and a tenant may only see their own lease's billing
        private async Task EnsureCanAccessLeaseAsync(int leaseId)
        {
            var tenantId = await _context.Leases
                .Where(l => l.Id == leaseId)
                .Select(l => (int?)l.TenantId)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException($"Lease {leaseId} not found");

            _currentUser.EnsureCanAccessTenant(tenantId);
        }

        private IQueryable<RentCharge> ChargesWithDetails() => _context.RentCharges
            .Include(c => c.Lease).ThenInclude(l => l.Tenant)
            .Include(c => c.Lease).ThenInclude(l => l.Apartment)
            .AsNoTracking();

        private static DateTime FirstOfMonth(DateTime date) => new DateTime(date.Year, date.Month, 1);
    }
}
