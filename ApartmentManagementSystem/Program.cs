using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Interfaces;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Models.Responses;
using ApartmentManagementSystem.Options;
using ApartmentManagementSystem.Security;
using ApartmentManagementSystem.Services;
using ApartmentManagementSystem.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// In containers/Azure, write one JSON object per log line so log tools can search by field
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        // Retry brief connection drops (Azure SQL, or SQL Server still starting in Docker)
        sql => sql.EnableRetryOnFailure()));

// --- Authentication (JWT) ---
// Fail at startup, not at first login, if the signing key isn't configured
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
jwtOptions.Validate();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep claim names as issued ("sub", "role", "tenantId")
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "email"
        };
    });
builder.Services.AddAuthorization();

// Auth endpoints: 10 requests per minute per client IP, then 429
// Note: counters are in memory per instance; move to a shared store if you run several instances
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// --- Validation ---
// FluentValidation validators in Validators/ run through ValidationFilter on every request body
builder.Services.AddValidatorsFromAssemblyContaining<ValidationFilter>();
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        // Let the validators decide what's required, instead of MVC treating every non-nullable string as [Required]
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    // Accept and return enums as text, e.g. "BankTransfer" instead of 1
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Malformed JSON or a wrong type (e.g. "abc" for a number) gets the same ErrorResponse body as everything else
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = context => ValidationFilter.BadRequest(
        context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

// --- Logging and health ---
// One line per request: method, path, status and duration. Headers and bodies are not logged (they hold tokens and passwords).
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Adds the "Authorize" button in Swagger UI for pasting a JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the access token from POST /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<IApartmentService, ApartmentService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ILeaseService, LeaseService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<RentBillingBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Docker Compose and the integration tests turn this on so a fresh database gets the schema and sample data.
    // Off by default: locally you run "dotnet ef database update" yourself.
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        app.Logger.LogInformation("Applying database migrations");
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;
        if (database.IsRelational())
            await database.MigrateAsync();
        else
            await database.EnsureCreatedAsync(); // in-memory test database has no migrations
    }

    await IdentitySeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Logger);
}

// First, so it also catches errors from auth and everything after it
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpLogging();

// Always on in Development; elsewhere only when Swagger:Enabled is set (e.g. the Docker demo)
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// GET /health: 200 "Healthy" when the API can reach the database, 503 otherwise. Used by Docker and Azure.
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.Run();

// Lets the integration tests start the app with WebApplicationFactory<Program>
public partial class Program { }
