using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class ApartmentsController : ControllerBase
    {
        private readonly IApartmentService _service;

        public ApartmentsController(IApartmentService service)
        {
            _service = service;
        }

        // GET: api/apartments?page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationQuery pagination)
        {
            var apartments = await _service.GetAllAsync(pagination);
            return Ok(apartments);
        }

        // GET: api/apartments/available?page=1&pageSize=10
        [HttpGet("available")]
        [AllowAnonymous] // public listing of vacant units
        public async Task<IActionResult> GetAvailable([FromQuery] PaginationQuery pagination)
        {
            var apartments = await _service.GetAvailableAsync(pagination);
            return Ok(apartments);
        }

        // GET: api/apartments/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var apartment = await _service.GetByIdAsync(id);
            if (apartment == null) return NotFound();
            return Ok(apartment);
        }

        // POST: api/apartments
        [HttpPost]
        public async Task<IActionResult> Create(Apartment apartment)
        {
            var created = await _service.CreateAsync(apartment);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/apartments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Apartment apartment)
        {
            if (id != apartment.Id) return BadRequest();

            var success = await _service.UpdateAsync(apartment);
            if (!success) return NotFound();
            return NoContent();
        }

        // DELETE: api/apartments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
    }
}
