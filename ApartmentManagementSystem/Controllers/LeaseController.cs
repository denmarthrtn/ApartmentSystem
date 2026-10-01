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
    [Authorize] // any logged-in user; tenants are limited to their own leases in LeaseService
    public class LeaseController : ControllerBase
    {
        private readonly ILeaseService _service;
        private readonly ICurrentUser _currentUser;

        public LeaseController(ILeaseService service, ICurrentUser currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        // GET: api/lease?page=1&pageSize=10
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationQuery pagination)
        {
            var leases = await _service.GetAllAsync(pagination);
            return Ok(leases.Map(ToDetailDto));
        }

        // GET: api/lease/mine?page=1&pageSize=10
        // The logged-in tenant's own leases
        [HttpGet("mine")]
        [Authorize(Roles = Roles.Tenant)]
        public async Task<IActionResult> GetMine([FromQuery] PaginationQuery pagination)
        {
            if (_currentUser.TenantId is not int tenantId) return Forbid();

            var leases = await _service.GetForTenantAsync(tenantId, pagination);
            return Ok(leases.Map(ToDetailDto));
        }

        // GET: api/lease/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var lease = await _service.GetByIdAsync(id);
            if (lease == null) return NotFound();
            return Ok(ToDetailDto(lease));
        }

        // POST: api/lease
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Create(LeaseCreateDto dto)
        {
            var lease = new Lease
            {
                TenantId = dto.TenantId,
                ApartmentId = dto.ApartmentId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MonthlyRent = dto.MonthlyRent,
                SecurityDeposit = dto.SecurityDeposit
            };

            var created = await _service.CreateAsync(lease);
            var result = ToDetailDto(created);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // POST: api/lease/5/terminate
        [HttpPost("{id}/terminate")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Terminate(int id, LeaseTerminateDto dto)
        {
            var lease = await _service.TerminateAsync(id, dto.TerminatedDate);
            return Ok(ToDetailDto(lease));
        }

        // Manual mapping: Lease -> LeaseDetailDto
        private static LeaseDetailDto ToDetailDto(Lease l) => new LeaseDetailDto
        {
            Id = l.Id,
            TenantId = l.TenantId,
            TenantName = l.Tenant?.Name ?? "N/A",
            ApartmentId = l.ApartmentId,
            ApartmentName = l.Apartment?.Name ?? "N/A",
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            TerminatedDate = l.TerminatedDate,
            MonthlyRent = l.MonthlyRent,
            SecurityDeposit = l.SecurityDeposit,
            Status = l.Status.ToString()
        };
    }
}
