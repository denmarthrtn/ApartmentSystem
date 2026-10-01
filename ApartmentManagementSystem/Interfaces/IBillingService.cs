using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Interfaces
{
    public interface IBillingService
    {
        Task<int> GenerateChargesAsync(DateTime asOf);
        Task<int> MarkOverdueAsync(DateTime asOf);
        Task<BillingRunResultDto> RunDailyBillingAsync(DateTime asOf);
        Task<Payment> RecordPaymentAsync(int rentChargeId, Payment payment);
        Task<PagedResult<RentCharge>> GetChargesForLeaseAsync(int leaseId, PaginationQuery pagination);
        Task<PagedResult<RentCharge>> GetOverdueChargesAsync(PaginationQuery pagination);
        Task<LeaseBalanceDto> GetLeaseBalanceAsync(int leaseId);
    }
}
