using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApartmentManagementSystem.Tests.Integration
{
    // Starts the real API in memory (same Program.cs, middleware, auth and validation),
    // with the EF InMemory database instead of SQL Server and the sample data from SeedData loaded.
    public class ApiFactory : WebApplicationFactory<Program>
    {
        public const string AdminEmail = "demo.admin@apartments.local";
        public const string AdminPassword = "Demo#Admin2026";
        public const string TenantEmail = "juan.delacruz@example.com";    // tenant 1001, active lease 1001 on apartment 1001
        public const string OtherTenantEmail = "maria.santos@example.com"; // tenant 1002, lease 1002
        public const string TenantPassword = "Tenant#2026x";

        private readonly string _databaseName = "IntegrationTests-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // UseSetting (not ConfigureAppConfiguration) because Program.cs reads these before the app is built
            builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-bytes!");
            builder.UseSetting("DemoMode:Enabled", "true");
            builder.UseSetting("Database:MigrateOnStartup", "true"); // loads the HasData sample data before the demo accounts

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));

                // No billing job running in the background while tests check numbers.
                // The real clock stays in place: JWT expiry is checked against it, so a fixed date would make tokens invalid.
                services.RemoveAll<IHostedService>(d => d.ImplementationType == typeof(RentBillingBackgroundService));
            });
        }

        public async Task<HttpClient> CreateClientAsAsync(string email, string password)
        {
            var client = CreateClient();
            var tokens = await LoginAsync(client, email, password);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            return client;
        }

        public static async Task<TokenResponseDto> LoginAsync(HttpClient client, string email, string password)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<TokenResponseDto>())!;
        }
    }

    internal static class ServiceCollectionTestExtensions
    {
        public static void RemoveAll<T>(this IServiceCollection services, Func<ServiceDescriptor, bool>? match = null)
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T) && (match == null || match(d))).ToList())
                services.Remove(descriptor);
        }
    }
}
