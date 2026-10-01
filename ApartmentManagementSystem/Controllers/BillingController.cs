using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // any logged-in user; tenants are limited to their own leases in BillingService
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _service;
        private readonly TimeProvider _timeProvider;

        public BillingController(IBillingService service, TimeProvider timeProvider)
        {
            _service = service;
            _timeProvider = timeProvider;
        }

        // GET: api/billing/leases/5/charges?page=1&pageSize=10
        [HttpGet("leases/{leaseId}/charges")]
        public async Task<IActionResult> GetChargesForLease(int leaseId, [FromQuery] PaginationQuery pagination)
        {
            var charges = await _service.GetChargesForLeaseAsync(leaseId, pagination);
            return Ok(charges.Map(ToChargeDto));
        }

        // GET: api/billing/leases/5/balance
        [HttpGet("leases/{leaseId}/balance")]
        public async Task<IActionResult> GetLeaseBalance(int leaseId)
        {
            return Ok(await _service.GetLeaseBalanceAsync(leaseId));
        }

        // GET: api/billing/charges/overdue?page=1&pageSize=10
        [HttpGet("charges/overdue")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetOverdue([FromQuery] PaginationQuery pagination)
        {
            var charges = await _service.GetOverdueChargesAsync(pagination);
            return Ok(charges.Map(ToChargeDto));
        }

        // POST: api/billing/charges/5/payments
        [HttpPost("charges/{chargeId}/payments")]
        [Authorize(Roles = Roles.Admin)] // the landlord records payments received
        public async Task<IActionResult> RecordPayment(int chargeId, PaymentCreateDto dto)
        {
            var payment = new Payment
            {
                Amount = dto.Amount,
                PaidDate = dto.PaidDate,
                Method = dto.Method,
                ReferenceNumber = dto.ReferenceNumber
            };

            var created = await _service.RecordPaymentAsync(chargeId, payment);
            return Ok(ToPaymentDto(created));
        }

        // POST: api/billing/run
        // Runs the same billing the background job does, on demand (handy for testing/demo)
        [HttpPost("run")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> RunBilling()
        {
            var result = await _service.RunDailyBillingAsync(_timeProvider.GetLocalNow().DateTime);
            return Ok(result);
        }

        // Manual mapping: RentCharge -> RentChargeDto
        private static RentChargeDto ToChargeDto(RentCharge c) => new RentChargeDto
        {
            Id = c.Id,
            LeaseId = c.LeaseId,
            TenantName = c.Lease?.Tenant?.Name ?? "N/A",
            ApartmentName = c.Lease?.Apartment?.Name ?? "N/A",
            BillingMonth = c.BillingMonth,
            DueDate = c.DueDate,
            Amount = c.Amount,
            AmountPaid = c.AmountPaid,
            Balance = c.Balance,
            Status = c.Status.ToString()
        };

        // Manual mapping: Payment -> PaymentDto
        private static PaymentDto ToPaymentDto(Payment p) => new PaymentDto
        {
            Id = p.Id,
            RentChargeId = p.RentChargeId,
            Amount = p.Amount,
            PaidDate = p.PaidDate,
            Method = p.Method.ToString(),
            ReferenceNumber = p.ReferenceNumber,
            ChargeBalance = p.RentCharge?.Balance ?? 0,
            ChargeStatus = p.RentCharge?.Status.ToString() ?? "N/A"
        };
    }
}
