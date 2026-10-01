using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentManagementSystem.Tests
{
    public class TenantServiceTests
    {
        private TenantService GetService(out ApplicationDbContext context)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
                .Options;

            context = new ApplicationDbContext(options);
            return new TenantService(context);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTenants()
        {
            var service = GetService(out var context);

            // Seed apartment
            var apartment = new Apartment
            {
                Name = "Sunset Apartments",
                Location = "City Center",
                Address = "Zone 6",
                Rent = 1000,
                Rooms = 2,
                HasParking = true
            };
            context.Apartments.Add(apartment);
            await context.SaveChangesAsync();

            await service.CreateAsync(new Tenant
            {
                Name = "Test tenant",
                Email = "Tenant@test.com",
                PhoneNumber = "123456789",
                PermanentAddress = "Permanend Address Sample",
                MoveInDate = DateTime.UtcNow,
                ApartmentId = apartment.Id
            });

            var result = await service.GetAllAsync(new PaginationQuery());

            Assert.Single(result.Items);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyTenant()
        {
            var service = GetService(out var context);

            var apartment = new Apartment
            {
                Name = "Elm Apartments",
                Location = "Downtown",
                Address = "Zone 6",
                Rent = 800,
                Rooms = 1,
                HasParking = false
            };
            context.Apartments.Add(apartment);
            await context.SaveChangesAsync();

            var tenant = await service.CreateAsync(new Tenant
            {
                Name = "Tenant",
                Email = "Tenant@test.com",
                PhoneNumber = "987654321",
                PermanentAddress = "Permanend Address Sample",
                MoveInDate = DateTime.UtcNow,
                ApartmentId = apartment.Id
            });

            tenant.Name = "New Name";
            var success = await service.UpdateAsync(tenant);

            Assert.True(success);
            var updated = await service.GetByIdAsync(tenant.Id);
            Assert.Equal("New Name", updated.Name);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveTenant()
        {
            var service = GetService(out var context);

            var apartment = new Apartment
            {
                Name = "Oak Apartments",
                Location = "Suburbs",
                Address = "Zone 6",
                Rent = 1200,
                Rooms = 3,
                HasParking = true
            };
            context.Apartments.Add(apartment);
            await context.SaveChangesAsync();

            var tenant = await service.CreateAsync(new Tenant
            {
                Name = "ToDelete",
                Email = "ToBeDeleted@test.com",
                PhoneNumber = "555555555",
                PermanentAddress = "Permanend Address Sample",
                MoveInDate = DateTime.UtcNow,
                ApartmentId = apartment.Id
            });

            var success = await service.DeleteAsync(tenant.Id);

            Assert.True(success);
            var deleted = await service.GetByIdAsync(tenant.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenNameIsEmpty()
        {
            var service = GetService(out var context);

            var apartment = new Apartment
            {
                Name = "Pine Apartments",
                Location = "Uptown",
                Address = "Zone 6",
                Rent = 900,
                Rooms = 2,
                HasParking = false
            };
            context.Apartments.Add(apartment);
            await context.SaveChangesAsync();

            var tenant = new Tenant
            {
                Name = "",
                Email = "Cebu@test.com",
                PhoneNumber = "111111111",
                PermanentAddress = "Permanend Address Sample",
                MoveInDate = DateTime.UtcNow,
                ApartmentId = apartment.Id
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(tenant));
        }
    }
}

