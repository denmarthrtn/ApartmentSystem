using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Extensions;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    public class TenantService : ITenantService
    {
        private readonly ApplicationDbContext _context;

        public TenantService (ApplicationDbContext context){
            _context = context;
        }

        public async Task<Tenant> CreateAsync(Tenant tenant)
        {
            if (string.IsNullOrWhiteSpace(tenant.Name))
                throw new ArgumentException("Name is required");
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            return tenant;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return false;

            _context.Tenants.Remove(tenant);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<PagedResult<Tenant>> GetAllAsync(PaginationQuery pagination)
        {
            return await _context.Tenants
                .Include(t => t.Apartment)
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .ToPagedResultAsync(pagination);
        }

        public async Task<Tenant?> GetByIdAsync(int id)
        {
            return await _context.Tenants.FindAsync(id);
        }

        public async Task<bool> UpdateAsync(Tenant tenant)
        {
            _context.Tenants.Update(tenant);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
