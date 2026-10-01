using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using ApartmentManagementSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    public class TenantController : ControllerBase
    {
        private readonly ITenantService _service;

        public TenantController(ITenantService service) {
            _service = service;
        }

        // GET: api/tenant?page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationQuery pagination)
        {
            var tenants = await _service.GetAllAsync(pagination);
            return Ok(tenants.Map(ToListDto));
        }

        // GET: api/tenant/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tenant = await _service.GetByIdAsync(id);
            if (tenant == null) return NotFound();
            return Ok(ToDetailDto(tenant));
        }

        // POST: api/tenant
        [HttpPost]
        public async Task<IActionResult> Create(TenantCreateDto dto)
        {
            var tenant = new Tenant
            {
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PermanentAddress = dto.PermanentAddress,
                MoveInDate = dto.MoveInDate,
                ApartmentId = dto.ApartmentId
            };

            var created = await _service.CreateAsync(tenant);
            var result = ToDetailDto(created);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT: api/tenant/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TenantUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var tenant = await _service.GetByIdAsync(id);
            if (tenant == null) return NotFound();

            tenant.Name = dto.Name;
            tenant.Email = dto.Email;
            tenant.PhoneNumber = dto.PhoneNumber;
            tenant.PermanentAddress = dto.PermanentAddress;
            tenant.MoveInDate = dto.MoveInDate;
            tenant.MoveOutDate = dto.MoveOutDate;
            tenant.ApartmentId = dto.ApartmentId;

            var success = await _service.UpdateAsync(tenant);
            if (!success) return NotFound();

            return Ok(ToDetailDto(tenant));
        }

        // DELETE: api/tenant/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }

        // Manual mapping: Tenant -> TenantListDto
        private static TenantListDto ToListDto(Tenant t) => new TenantListDto
        {
            Id = t.Id,
            Name = t.Name,
            Email = t.Email,
            PhoneNumber = t.PhoneNumber,
            ApartmentName = t.Apartment?.Name ?? "N/A",
            ApartmentLocation = t.Apartment?.Location ?? "N/A"
        };

        // Manual mapping: Tenant -> TenantDetailDto
        private static TenantDetailDto ToDetailDto(Tenant t) => new TenantDetailDto
        {
            Id = t.Id,
            Name = t.Name,
            Email = t.Email,
            PhoneNumber = t.PhoneNumber,
            PermanentAddress = t.PermanentAddress,
            MoveInDate = t.MoveInDate,
            MoveOutDate = t.MoveOutDate,
            ApartmentId = t.ApartmentId,
            ApartmentName = t.Apartment?.Name ?? "N/A",
            ApartmentLocation = t.Apartment?.Location ?? "N/A"
        };
    }
}
