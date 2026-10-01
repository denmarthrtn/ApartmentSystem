using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Interfaces
{
    public interface IApartmentService
    {
        Task<PagedResult<Apartment>> GetAllAsync(PaginationQuery pagination);
        Task<PagedResult<Apartment>> GetAvailableAsync(PaginationQuery pagination);
        Task<Apartment?> GetByIdAsync(int id);
        Task<Apartment> CreateAsync(Apartment apartment);
        Task<bool> UpdateAsync(Apartment apartment);
        Task<bool> DeleteAsync(int id);
    }
}
