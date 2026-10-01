using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ApartmentServiceTests
{
    private ApartmentService GetService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        var context = new ApplicationDbContext(options);
        return new ApartmentService(context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnApartments()
    {
        var service = GetService();
        await service.CreateAsync(new Apartment { Name = "Test Apt", Location = "Cebu", Address = "Zone 6" });

        var result = await service.GetAllAsync(new PaginationQuery());

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyApartment()
    {
        var service = GetService();
        var apt = await service.CreateAsync(new Apartment { Name = "Old Name", Location = "QC", Address = "Zone 6" });

        apt.Name = "New Name";
        var success = await service.UpdateAsync(apt);

        Assert.True(success);
        var updated = await service.GetByIdAsync(apt.Id);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveApartment()
    {
        var service = GetService();
        var apt = await service.CreateAsync(new Apartment { Name = "ToDelete", Location = "Pasig", Address = "Zone 6" });

        var success = await service.DeleteAsync(apt.Id);

        Assert.True(success);
        var deleted = await service.GetByIdAsync(apt.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNameIsEmpty()
    {
        var service = GetService();
        var apartment = new Apartment { Name = "", Location = "Cebu", Address = "Zone 6", HasParking = true };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(apartment));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenParkingRequired()
    {
        var service = GetService();
        var apartment = new Apartment { Name = "Skyline", Location = "Makati", Address = "Zone 6", HasParking = false };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(apartment));
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenValid()
    {
        var service = GetService();
        var apartment = new Apartment { Name = "Skyline", Location = "Makati", Address = "Zone 6", HasParking = true };

        var result = await service.CreateAsync(apartment);

        Assert.NotNull(result);
        Assert.Equal("Skyline", result.Name);
    }


}
