using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentManagementSystem.Tests
{
    public class PaginationTests
    {
        private static async Task<ApartmentService> GetServiceWithApartmentsAsync(int count)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
                .Options;

            var context = new ApplicationDbContext(options);
            for (var i = 1; i <= count; i++)
            {
                context.Apartments.Add(new Apartment { Name = $"Apt {i}", Location = "Cebu", Address = "Zone 6", Rent = 1000, Rooms = 1 });
            }
            await context.SaveChangesAsync();

            return new ApartmentService(context);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRequestedPage()
        {
            var service = await GetServiceWithApartmentsAsync(25);

            var result = await service.GetAllAsync(new PaginationQuery { Page = 2, PageSize = 10 });

            Assert.Equal(10, result.Items.Count);
            Assert.Equal("Apt 11", result.Items.First().Name);
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
            Assert.True(result.HasNextPage);
            Assert.True(result.HasPreviousPage);
        }

        [Fact]
        public async Task GetAllAsync_LastPage_ShouldReturnRemainingItems()
        {
            var service = await GetServiceWithApartmentsAsync(25);

            var result = await service.GetAllAsync(new PaginationQuery { Page = 3, PageSize = 10 });

            Assert.Equal(5, result.Items.Count);
            Assert.False(result.HasNextPage);
        }

        [Fact]
        public async Task GetAllAsync_PageBeyondEnd_ShouldReturnEmpty()
        {
            var service = await GetServiceWithApartmentsAsync(5);

            var result = await service.GetAllAsync(new PaginationQuery { Page = 10, PageSize = 10 });

            Assert.Empty(result.Items);
            Assert.Equal(5, result.TotalCount);
        }

        [Fact]
        public void PaginationQuery_ShouldClampInvalidValues()
        {
            var query = new PaginationQuery { Page = 0, PageSize = 1000 };

            Assert.Equal(1, query.Page);
            Assert.Equal(PaginationQuery.MaxPageSize, query.PageSize);
        }
    }
}
