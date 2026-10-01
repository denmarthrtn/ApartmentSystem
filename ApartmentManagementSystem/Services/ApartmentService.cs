using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    public class ApartmentService : IApartmentService
    {
        private readonly ApplicationDbContext _context;

        public ApartmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Apartment> CreateAsync(Apartment apartment)
        {
            if (string.IsNullOrWhiteSpace(apartment.Name))
                throw new ArgumentException("Name is required");

            if (!apartment.HasParking && apartment.Location == "Makati")
                throw new InvalidOperationException("Parking required in Makati");

            _context.Apartments.Add(apartment);
            await _context.SaveChangesAsync();
            return apartment;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var apt = await _context.Apartments.FindAsync(id);
            if (apt == null) return false;

            _context.Apartments.Remove(apt);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<PagedResult<Apartment>> GetAllAsync(PaginationQuery pagination)
        {
            return await _context.Apartments
                .AsNoTracking()
                .OrderBy(a => a.Id)
                .ToPagedResultAsync(pagination);
        }

        // Apartments with no active lease
        public async Task<PagedResult<Apartment>> GetAvailableAsync(PaginationQuery pagination)
        {
            return await _context.Apartments
                .AsNoTracking()
                .Where(a => !_context.Leases.Any(l => l.ApartmentId == a.Id && l.Status == LeaseStatus.Active))
                .OrderBy(a => a.Id)
                .ToPagedResultAsync(pagination);
        }

        public async Task<Apartment?> GetByIdAsync(int id)
        {
            return await _context.Apartments.FindAsync(id);
        }

        public async Task<bool> UpdateAsync(Apartment apartment)
        {
            _context.Apartments.Update(apartment);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
