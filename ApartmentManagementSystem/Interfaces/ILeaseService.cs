using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Interfaces
{
    public interface ILeaseService
    {
        Task<PagedResult<Lease>> GetAllAsync(PaginationQuery pagination);
        Task<PagedResult<Lease>> GetForTenantAsync(int tenantId, PaginationQuery pagination);
        Task<Lease?> GetByIdAsync(int id);
        Task<Lease> CreateAsync(Lease lease);
        Task<Lease> TerminateAsync(int id, DateTime terminatedDate);
    }
}
