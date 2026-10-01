using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Extensions;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        public const int MaxTitleLength = 100;
        public const int MaxDescriptionLength = 2000;

        private readonly ApplicationDbContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _timeProvider;

        public MaintenanceService(ApplicationDbContext context, ICurrentUser currentUser, TimeProvider timeProvider)
        {
            _context = context;
            _currentUser = currentUser;
            _timeProvider = timeProvider;
        }

        public async Task<MaintenanceRequest> CreateAsync(MaintenanceCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Title is required");
            if (dto.Title.Length > MaxTitleLength)
                throw new ArgumentException($"Title cannot be longer than {MaxTitleLength} characters");
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Description is required");
            if (dto.Description.Length > MaxDescriptionLength)
                throw new ArgumentException($"Description cannot be longer than {MaxDescriptionLength} characters");
            if (!Enum.IsDefined(dto.Priority))
                throw new ArgumentException("Invalid priority");

            int apartmentId;
            int? tenantId;

            if (_currentUser.IsAdmin)
            {
                // Admin logs a request for any apartment (e.g. a phone call, or a vacant unit)
                apartmentId = dto.ApartmentId ?? throw new ArgumentException("ApartmentId is required");
                if (!await _context.Apartments.AnyAsync(a => a.Id == apartmentId))
                    throw new KeyNotFoundException($"Apartment {apartmentId} not found");
                tenantId = null;
            }
            else
            {
                // A tenant can only report problems in the apartment they currently lease
                tenantId = _currentUser.TenantId
                    ?? throw new UnauthorizedAccessException("Only tenants and admins can create maintenance requests");

                var leasedApartmentId = await _context.Leases
                    .Where(l => l.TenantId == tenantId && l.Status == LeaseStatus.Active)
                    .Select(l => (int?)l.ApartmentId)
                    .FirstOrDefaultAsync()
                    ?? throw new InvalidOperationException("You need an active lease to submit a maintenance request");

                if (dto.ApartmentId.HasValue && dto.ApartmentId != leasedApartmentId)
                    throw new UnauthorizedAccessException("You can only report problems in the apartment you lease");

                apartmentId = leasedApartmentId;
            }

            var now = Now();
            var request = new MaintenanceRequest
            {
                ApartmentId = apartmentId,
                TenantId = tenantId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Priority = dto.Priority,
                Status = MaintenanceStatus.Open,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            await _context.Entry(request).Reference(r => r.Apartment).LoadAsync();
            if (request.TenantId.HasValue)
                await _context.Entry(request).Reference(r => r.Tenant).LoadAsync();
            return request;
        }

        // Admin work list: most urgent first, then oldest first
        public async Task<PagedResult<MaintenanceRequest>> GetAllAsync(MaintenanceQuery query)
        {
            return await Filter(WithDetails(), query)
                .OrderByDescending(r => r.Priority)
                .ThenBy(r => r.CreatedAt)
                .ThenBy(r => r.Id)
                .ToPagedResultAsync(query);
        }

        public async Task<PagedResult<MaintenanceRequest>> GetForTenantAsync(int tenantId, MaintenanceQuery query)
        {
            _currentUser.EnsureCanAccessTenant(tenantId);

            return await Filter(WithDetails(), query)
                .Where(r => r.TenantId == tenantId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenBy(r => r.Id)
                .ToPagedResultAsync(query);
        }

        public async Task<MaintenanceRequest?> GetByIdAsync(int id)
        {
            var request = await WithDetails().FirstOrDefaultAsync(r => r.Id == id);
            if (request != null)
                EnsureCanAccess(request);
            return request;
        }

        public async Task<MaintenanceRequest> UpdateStatusAsync(int id, MaintenanceStatus status, string? resolutionNotes)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Apartment)
                .Include(r => r.Tenant)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Maintenance request {id} not found");

            if (!Enum.IsDefined(status))
                throw new ArgumentException("Invalid status");
            if (!MaintenanceRequest.CanMove(request.Status, status))
                throw new InvalidOperationException($"Cannot change status from {request.Status} to {status}");
            if (status == MaintenanceStatus.Resolved && string.IsNullOrWhiteSpace(resolutionNotes))
                throw new ArgumentException("Resolution notes are required when resolving a request");

            var now = Now();
            request.Status = status;
            request.UpdatedAt = now;
            if (status == MaintenanceStatus.Resolved)
            {
                request.ResolvedAt = now;
                request.ResolutionNotes = resolutionNotes!.Trim();
            }

            await _context.SaveChangesAsync();
            return request;
        }

        // Tenants can withdraw their own request as long as work hasn't started
        public async Task<MaintenanceRequest> CancelAsync(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Apartment)
                .Include(r => r.Tenant)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Maintenance request {id} not found");

            EnsureCanAccess(request);

            if (request.Status != MaintenanceStatus.Open)
                throw new InvalidOperationException("Only open requests can be cancelled");

            request.Status = MaintenanceStatus.Cancelled;
            request.UpdatedAt = Now();
            await _context.SaveChangesAsync();
            return request;
        }

        private void EnsureCanAccess(MaintenanceRequest request)
        {
            if (_currentUser.IsAdmin) return;
            if (request.TenantId is not int tenantId)
                throw new UnauthorizedAccessException("You can only access your own records");

            _currentUser.EnsureCanAccessTenant(tenantId);
        }

        private static IQueryable<MaintenanceRequest> Filter(IQueryable<MaintenanceRequest> requests, MaintenanceQuery query)
        {
            if (query.Status.HasValue)
                requests = requests.Where(r => r.Status == query.Status);
            if (query.Priority.HasValue)
                requests = requests.Where(r => r.Priority == query.Priority);
            return requests;
        }

        private IQueryable<MaintenanceRequest> WithDetails() => _context.MaintenanceRequests
            .Include(r => r.Apartment)
            .Include(r => r.Tenant)
            .AsNoTracking();

        private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
