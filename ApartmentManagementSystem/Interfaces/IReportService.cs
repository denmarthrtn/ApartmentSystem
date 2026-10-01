using ApartmentManagementSystem.DTOs;

namespace ApartmentManagementSystem.Interfaces
{
    public interface IReportService
    {
        Task<ReportSummaryDto> GetSummaryAsync();
    }
}
