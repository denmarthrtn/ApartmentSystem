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
            return Ok(apartments.Map(ToListDto));
        }

        // GET: api/apartments/available?page=1&pageSize=10
        [HttpGet("available")]
        [AllowAnonymous] // public listing of vacant units
        public async Task<IActionResult> GetAvailable([FromQuery] PaginationQuery pagination)
        {
            var apartments = await _service.GetAvailableAsync(pagination);
            return Ok(apartments.Map(ToListDto));
        }

        // GET: api/apartments/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var apartment = await _service.GetByIdAsync(id);
            if (apartment == null) return NotFound();
            return Ok(ToDetailDto(apartment));
        }

        // POST: api/apartments
        [HttpPost]
        public async Task<IActionResult> Create(ApartmentCreateDto dto)
        {
            var apartment = new Apartment
            {
                Name = dto.Name,
                Location = dto.Location,
                Address = dto.Address,
                Rent = dto.Rent,
                Rooms = dto.Rooms,
                HasParking = dto.HasParking
            };

            var created = await _service.CreateAsync(apartment);
            var result = ToDetailDto(created);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT: api/apartments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ApartmentUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var apartment = await _service.GetByIdAsync(id);
            if (apartment == null) return NotFound();

            apartment.Name = dto.Name;
            apartment.Location = dto.Location;
            apartment.Address = dto.Address;
            apartment.Rent = dto.Rent;
            apartment.Rooms = dto.Rooms;
            apartment.HasParking = dto.HasParking;

            var success = await _service.UpdateAsync(apartment);
            if (!success) return NotFound();

            return Ok(ToDetailDto(apartment));
        }

        // DELETE: api/apartments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }

        // Manual mapping: Apartment -> ApartmentListDto
        private static ApartmentListDto ToListDto(Apartment a) => new ApartmentListDto
        {
            Id = a.Id,
            Name = a.Name,
            Location = a.Location,
            Rent = a.Rent,
            Rooms = a.Rooms,
            HasParking = a.HasParking
        };

        // Manual mapping: Apartment -> ApartmentDetailDto
        private static ApartmentDetailDto ToDetailDto(Apartment a) => new ApartmentDetailDto
        {
            Id = a.Id,
            Name = a.Name,
            Location = a.Location,
            Address = a.Address,
            Rent = a.Rent,
            Rooms = a.Rooms,
            HasParking = a.HasParking
        };
    }
}
