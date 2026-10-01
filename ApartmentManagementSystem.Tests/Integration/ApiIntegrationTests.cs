using System.Net;
using System.Net.Http.Json;
using System.Text;
using ApartmentManagementSystem.DTOs;
using ApartmentManagementSystem.Models.Responses;

namespace ApartmentManagementSystem.Tests.Integration
{
    // Calls the real endpoints over HTTP. Each test gets its own app and database,
    // so tests that create data don't change the numbers other tests check.
    public class ApiIntegrationTests : IAsyncLifetime
    {
        private ApiFactory _factory = null!;

        public Task InitializeAsync()
        {
            _factory = new ApiFactory();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync() => await _factory.DisposeAsync();

        private Task<HttpClient> AdminAsync() => _factory.CreateClientAsAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        private Task<HttpClient> TenantAsync() => _factory.CreateClientAsAsync(ApiFactory.TenantEmail, ApiFactory.TenantPassword);

        // --- Health and public endpoints ---

        [Fact]
        public async Task Health_ShouldReturnHealthy()
        {
            var response = await _factory.CreateClient().GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task AvailableApartments_ShouldBePublic()
        {
            var response = await _factory.CreateClient().GetAsync("/api/apartments/available");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // --- Apartments ---

        [Fact]
        public async Task Admin_CreatesAndUpdatesApartment_ThroughDtos()
        {
            var admin = await AdminAsync();

            var createResponse = await admin.PostAsJsonAsync("/api/apartments", new
            {
                name = "Unit 9A", location = "Makati", address = "9 Ayala Ave", rent = 18000m, rooms = 2, hasParking = true
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var created = await createResponse.Content.ReadFromJsonAsync<ApartmentDetailDto>();
            Assert.Equal("Unit 9A", created!.Name);

            var updateResponse = await admin.PutAsJsonAsync($"/api/apartments/{created.Id}", new
            {
                id = created.Id, name = "Unit 9A", location = "Makati", address = "9 Ayala Ave", rent = 19500m, rooms = 2, hasParking = false
            });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            // The public detail endpoint returns the updated values
            var fetched = await _factory.CreateClient().GetFromJsonAsync<ApartmentDetailDto>($"/api/apartments/{created.Id}");
            Assert.Equal(19500m, fetched!.Rent);
            Assert.False(fetched.HasParking);
        }

        [Fact]
        public async Task CreateApartment_WithMissingFields_ShouldReturn400()
        {
            var admin = await AdminAsync();

            var response = await admin.PostAsJsonAsync("/api/apartments", new { rent = 0, rooms = 0 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // --- Authentication ---

        [Fact]
        public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
        {
            var response = await _factory.CreateClient().GetAsync("/api/reports/summary");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithWrongPassword_ShouldReturn401()
        {
            var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
                new { email = ApiFactory.AdminEmail, password = "wrong-password" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_ShouldWorkOnce_AndBeRejectedWhenReused()
        {
            var client = _factory.CreateClient();
            var tokens = await ApiFactory.LoginAsync(client, ApiFactory.TenantEmail, ApiFactory.TenantPassword);

            var first = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });
            var reused = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        }

        // --- Roles and ownership ---

        [Fact]
        public async Task Tenant_GettingReportSummary_ShouldReturn403()
        {
            var client = await TenantAsync();

            var response = await client.GetAsync("/api/reports/summary");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Tenant_GettingAnotherTenantsLease_ShouldReturn403()
        {
            var client = await TenantAsync();

            var own = await client.GetAsync("/api/lease/1001");
            var other = await client.GetAsync("/api/lease/1002");

            Assert.Equal(HttpStatusCode.OK, own.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
            var error = await other.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal(403, error!.StatusCode);
        }

        // --- Validation ---

        [Fact]
        public async Task CreateLease_WithEndBeforeStart_ShouldReturn400WithErrorResponse()
        {
            var client = await AdminAsync();

            var response = await client.PostAsJsonAsync("/api/lease", new
            {
                tenantId = 1003,
                apartmentId = 1005,
                startDate = "2026-11-01",
                endDate = "2026-10-01",
                monthlyRent = 15000,
                securityDeposit = 30000
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal(400, error!.StatusCode);
            Assert.Equal("End date must be after start date", error.Message);
        }

        [Fact]
        public async Task CreateMaintenance_WithMissingFields_ShouldListEveryError()
        {
            var client = await TenantAsync();

            var response = await client.PostAsJsonAsync("/api/maintenance", new { priority = "High" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal("One or more fields are invalid", error!.Message);
            Assert.Contains("Title", error.Details);
            Assert.Contains("Description", error.Details);
        }

        [Fact]
        public async Task MalformedJson_ShouldReturn400WithErrorResponse()
        {
            var client = await AdminAsync();
            var body = new StringContent("{ \"monthlyRent\": \"lots\" ", Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/lease", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal(400, error!.StatusCode);
        }

        // --- End to end ---

        [Fact]
        public async Task Tenant_CreatesMaintenanceRequest_AdminResolvesIt()
        {
            var tenant = await TenantAsync();
            var admin = await AdminAsync();

            var created = await tenant.PostAsJsonAsync("/api/maintenance",
                new { title = "Clogged sink", description = "Bathroom sink drains slowly", priority = "High" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var request = (await created.Content.ReadFromJsonAsync<MaintenanceRequestDto>())!;
            Assert.Equal(1001, request.ApartmentId);   // taken from the tenant's active lease
            Assert.Equal("Open", request.Status);

            // Skipping In Progress isn't allowed
            var skip = await admin.PatchAsJsonAsync($"/api/maintenance/{request.Id}/status",
                new { status = "Resolved", resolutionNotes = "Cleared the drain" });
            Assert.Equal(HttpStatusCode.Conflict, skip.StatusCode);

            await admin.PatchAsJsonAsync($"/api/maintenance/{request.Id}/status", new { status = "InProgress" });
            var resolved = await admin.PatchAsJsonAsync($"/api/maintenance/{request.Id}/status",
                new { status = "Resolved", resolutionNotes = "Cleared the drain" });
            Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);

            var mine = await tenant.GetFromJsonAsync<PagedResult<MaintenanceRequestDto>>("/api/maintenance/mine?status=Resolved");
            Assert.Contains(mine!.Items, r => r.Id == request.Id && r.ResolutionNotes == "Cleared the drain");
        }

        [Fact]
        public async Task Admin_ReportSummary_ShouldReflectSampleData()
        {
            var client = await AdminAsync();

            var summary = await client.GetFromJsonAsync<ReportSummaryDto>("/api/reports/summary");

            // SeedData: 5 apartments, active leases on 1001, 1002 and 1004
            Assert.Equal(5, summary!.TotalApartments);
            Assert.Equal(3, summary.OccupiedApartments);
            Assert.Equal(60m, summary.OccupancyRate);
            Assert.Equal(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), summary.Month);
            // Sample requests: 3 open (one urgent), 1 in progress, 1 resolved
            Assert.Equal(3, summary.OpenMaintenanceRequests);
            Assert.Equal(1, summary.InProgressMaintenanceRequests);
            Assert.Equal(1, summary.UrgentMaintenanceRequests);
        }
    }
}
