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
    [Authorize] // any logged-in user; tenants are limited to their own requests in MaintenanceService
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceService _service;
        private readonly ICurrentUser _currentUser;

        public MaintenanceController(IMaintenanceService service, ICurrentUser currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        // GET: api/maintenance?status=Open&priority=Urgent&page=1&pageSize=10
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAll([FromQuery] MaintenanceQuery query)
        {
            var requests = await _service.GetAllAsync(query);
            return Ok(requests.Map(ToDto));
        }

        // GET: api/maintenance/mine?status=Open
        [HttpGet("mine")]
        [Authorize(Roles = Roles.Tenant)]
        public async Task<IActionResult> GetMine([FromQuery] MaintenanceQuery query)
        {
            if (_currentUser.TenantId is not int tenantId) return Forbid();

            var requests = await _service.GetForTenantAsync(tenantId, query);
            return Ok(requests.Map(ToDto));
        }

        // GET: api/maintenance/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _service.GetByIdAsync(id);
            if (request == null) return NotFound();
            return Ok(ToDto(request));
        }

        // POST: api/maintenance
        [HttpPost]
        public async Task<IActionResult> Create(MaintenanceCreateDto dto)
        {
            var created = await _service.CreateAsync(dto);
            var result = ToDto(created);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PATCH: api/maintenance/5/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateStatus(int id, MaintenanceStatusUpdateDto dto)
        {
            var request = await _service.UpdateStatusAsync(id, dto.Status, dto.ResolutionNotes);
            return Ok(ToDto(request));
        }

        // POST: api/maintenance/5/cancel
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var request = await _service.CancelAsync(id);
            return Ok(ToDto(request));
        }

        // Manual mapping: MaintenanceRequest -> MaintenanceRequestDto
        private static MaintenanceRequestDto ToDto(MaintenanceRequest r) => new MaintenanceRequestDto
        {
            Id = r.Id,
            ApartmentId = r.ApartmentId,
            ApartmentName = r.Apartment?.Name ?? "N/A",
            TenantId = r.TenantId,
            TenantName = r.Tenant?.Name ?? "N/A",
            Title = r.Title,
            Description = r.Description,
            Priority = r.Priority.ToString(),
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            ResolvedAt = r.ResolvedAt,
            ResolutionNotes = r.ResolutionNotes
        };
    }
}
