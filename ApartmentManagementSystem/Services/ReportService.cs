using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _timeProvider;

        public ReportService(ApplicationDbContext context, ICurrentUser currentUser, TimeProvider timeProvider)
        {
            _context = context;
            _currentUser = currentUser;
            _timeProvider = timeProvider;
        }

        public async Task<ReportSummaryDto> GetSummaryAsync()
        {
            // Totals across all tenants, so landlord only (checked here, not just in the controller)
            if (!_currentUser.IsAdmin)
                throw new UnauthorizedAccessException("Only admins can view reports");

            // Same clock as billing, so "this month" matches the month the charges were created for
            var today = _timeProvider.GetLocalNow().DateTime.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var totalApartments = await _context.Apartments.CountAsync();
            var occupiedApartments = await _context.Leases
                .Where(l => l.Status == LeaseStatus.Active)
                .Select(l => l.ApartmentId)
                .Distinct()
                .CountAsync();

            var rentBilled = await _context.RentCharges
                .Where(c => c.BillingMonth == monthStart)
                .SumAsync(c => c.Amount);
            var rentCollected = await _context.Payments
                .Where(p => p.PaidDate >= monthStart && p.PaidDate < nextMonthStart)
                .SumAsync(p => p.Amount);

            var overdue = _context.RentCharges.Where(c => c.Status == RentChargeStatus.Overdue);
            var overdueCount = await overdue.CountAsync();
            var totalOverdue = await overdue.SumAsync(c => c.Amount - c.AmountPaid); // Balance isn't a DB column

            var maintenanceByStatus = await _context.MaintenanceRequests
                .Where(r => r.Status == MaintenanceStatus.Open || r.Status == MaintenanceStatus.InProgress)
                .GroupBy(r => new { r.Status, Urgent = r.Priority == MaintenancePriority.Urgent })
                .Select(g => new { g.Key.Status, g.Key.Urgent, Count = g.Count() })
                .ToListAsync();

            return new ReportSummaryDto
            {
                AsOf = today,
                Month = monthStart,
                TotalApartments = totalApartments,
                OccupiedApartments = occupiedApartments,
                VacantApartments = totalApartments - occupiedApartments,
                OccupancyRate = totalApartments == 0
                    ? 0
                    : Math.Round(occupiedApartments * 100m / totalApartments, 2),
                RentBilledThisMonth = rentBilled,
                RentCollectedThisMonth = rentCollected,
                OverdueCharges = overdueCount,
                TotalOverdue = totalOverdue,
                OpenMaintenanceRequests = maintenanceByStatus.Where(m => m.Status == MaintenanceStatus.Open).Sum(m => m.Count),
                InProgressMaintenanceRequests = maintenanceByStatus.Where(m => m.Status == MaintenanceStatus.InProgress).Sum(m => m.Count),
                UrgentMaintenanceRequests = maintenanceByStatus.Where(m => m.Urgent).Sum(m => m.Count)
            };
        }
    }
}
