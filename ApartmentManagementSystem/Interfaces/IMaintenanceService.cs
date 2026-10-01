using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Interfaces
{
    public interface IMaintenanceService
    {
        Task<MaintenanceRequest> CreateAsync(MaintenanceCreateDto dto);
        Task<PagedResult<MaintenanceRequest>> GetAllAsync(MaintenanceQuery query);
        Task<PagedResult<MaintenanceRequest>> GetForTenantAsync(int tenantId, MaintenanceQuery query);
        Task<MaintenanceRequest?> GetByIdAsync(int id);
        Task<MaintenanceRequest> UpdateStatusAsync(int id, MaintenanceStatus status, string? resolutionNotes);
        Task<MaintenanceRequest> CancelAsync(int id);
    }
}
