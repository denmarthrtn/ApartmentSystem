using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Extensions;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    public class LeaseService : ILeaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUser _currentUser;

        public LeaseService(ApplicationDbContext context, ICurrentUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PagedResult<Lease>> GetForTenantAsync(int tenantId, PaginationQuery pagination)
        {
            _currentUser.EnsureCanAccessTenant(tenantId);

            return await _context.Leases
                .Include(l => l.Tenant)
                .Include(l => l.Apartment)
                .AsNoTracking()
                .Where(l => l.TenantId == tenantId)
                .OrderByDescending(l => l.StartDate)
                .ThenBy(l => l.Id)
                .ToPagedResultAsync(pagination);
        }

        public async Task<PagedResult<Lease>> GetAllAsync(PaginationQuery pagination)
        {
            return await _context.Leases
                .Include(l => l.Tenant)
                .Include(l => l.Apartment)
                .AsNoTracking()
                .OrderBy(l => l.Id)
                .ToPagedResultAsync(pagination);
        }

        public async Task<Lease?> GetByIdAsync(int id)
        {
            var lease = await _context.Leases
                .Include(l => l.Tenant)
                .Include(l => l.Apartment)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lease != null)
                _currentUser.EnsureCanAccessTenant(lease.TenantId);

            return lease;
        }

        public async Task<Lease> CreateAsync(Lease lease)
        {
            if (lease.EndDate <= lease.StartDate)
                throw new ArgumentException("End date must be after start date");
            if (lease.MonthlyRent <= 0)
                throw new ArgumentException("Monthly rent must be greater than zero");
            if (lease.SecurityDeposit < 0)
                throw new ArgumentException("Security deposit cannot be negative");

            if (!await _context.Tenants.AnyAsync(t => t.Id == lease.TenantId))
                throw new KeyNotFoundException($"Tenant {lease.TenantId} not found");
            if (!await _context.Apartments.AnyAsync(a => a.Id == lease.ApartmentId))
                throw new KeyNotFoundException($"Apartment {lease.ApartmentId} not found");

            var occupied = await _context.Leases.AnyAsync(l =>
                l.ApartmentId == lease.ApartmentId && l.Status == LeaseStatus.Active);
            if (occupied)
                throw new InvalidOperationException($"Apartment {lease.ApartmentId} already has an active lease");

            lease.Status = LeaseStatus.Active;
            lease.TerminatedDate = null;

            _context.Leases.Add(lease);
            await _context.SaveChangesAsync();

            // Load navigation properties so the response includes names
            await _context.Entry(lease).Reference(l => l.Tenant).LoadAsync();
            await _context.Entry(lease).Reference(l => l.Apartment).LoadAsync();
            return lease;
        }

        public async Task<Lease> TerminateAsync(int id, DateTime terminatedDate)
        {
            var lease = await GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Lease {id} not found");

            if (lease.Status != LeaseStatus.Active)
                throw new InvalidOperationException("Only active leases can be terminated");
            if (terminatedDate < lease.StartDate || terminatedDate > lease.EndDate)
                throw new ArgumentException("Termination date must be within the lease period");

            lease.Status = LeaseStatus.Terminated;
            lease.TerminatedDate = terminatedDate;
            await _context.SaveChangesAsync();
            return lease;
        }
    }
}
