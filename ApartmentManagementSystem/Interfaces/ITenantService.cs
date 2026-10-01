using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Interfaces
{
    public interface ITenantService
    {
        Task<PagedResult<Tenant>> GetAllAsync(PaginationQuery pagination);
        Task<Tenant?> GetByIdAsync(int id);
        Task<Tenant> CreateAsync(Tenant tenant);
        Task<bool> UpdateAsync(Tenant tenant);
        Task<bool> DeleteAsync(int id);
    }
}
