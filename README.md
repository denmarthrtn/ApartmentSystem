# Apartment Management System

[![CI](https://github.com/denmarthrtn/ApartmentSystem/actions/workflows/ci.yml/badge.svg)](https://github.com/denmarthrtn/ApartmentSystem/actions/workflows/ci.yml)

An ASP.NET Core 10 Web API for a small property manager: apartments, tenants, leases, monthly rent, payments, maintenance requests and reports.

**Live demo:** [Swagger UI](https://apartment-den-fec9b9dre5dadsg9.eastasia-01.azurewebsites.net/swagger) · [vacant apartments (public endpoint)](https://apartment-den-fec9b9dre5dadsg9.eastasia-01.azurewebsites.net/api/apartments/available) · [health check](https://apartment-den-fec9b9dre5dadsg9.eastasia-01.azurewebsites.net/health)

The first request can take a few seconds while the app wakes up.

## Features

- **Apartments and tenants.** Create, update and delete them. The list of vacant apartments is public.
- **Leases.** Each lease links a tenant to an apartment, and an apartment can only have one active lease. Leases can be ended early.
- **Rent billing.** A monthly charge for every active lease, due on the 5th. A background job runs at startup and then daily, creating any missing charges and marking unpaid ones as overdue. Running it twice never creates duplicate charges.
- **Payments.** A charge can be paid in parts (cash, bank transfer or e-wallet). Balances and overdue charges have their own endpoints.
- **Maintenance requests.** Tenants report problems and the admin moves them through Open → In Progress → Resolved.
- **Reports.** One summary for the admin: occupancy, rent billed and collected this month, overdue totals and open maintenance.
- **Security.** Login with JWT access tokens and refresh tokens, with two roles: Admin and Tenant. Tenants can only see their own records.
- **Paging, validation and errors.** Every list is paged. Request bodies are checked with FluentValidation, and all errors come back in the same JSON format. `/health` checks the database.

## Tech stack

C# · .NET 10 (LTS) · ASP.NET Core · Entity Framework Core · SQL Server · ASP.NET Core Identity + JWT · FluentValidation · xUnit · Docker · GitHub Actions

## Getting started

**You need:** the [.NET 10 SDK](https://dotnet.microsoft.com/download), SQL Server LocalDB, and the EF Core tool (`dotnet tool install --global dotnet-ef`).

```bash
# 1. Secrets are kept in user-secrets, never in appsettings
dotnet user-secrets set "Jwt:SigningKey" "<at least 32 random characters>" --project ApartmentManagementSystem
dotnet user-secrets set "SeedAdmin:Email" "you@example.com" --project ApartmentManagementSystem      # optional
dotnet user-secrets set "SeedAdmin:Password" "<a strong password>" --project ApartmentManagementSystem  # optional

# 2. Create the database
dotnet ef database update --project ApartmentManagementSystem

# 3. Run
dotnet run --project ApartmentManagementSystem
```

Open `/swagger` on the URL the app prints. Log in with `POST /api/auth/login`, then click **Authorize** and paste the access token.

### Run with Docker

```bash
cp .env.example .env    # fill in the values
docker compose up --build
```

This starts SQL Server and the API. Swagger is at http://localhost:8080/swagger and the health check at http://localhost:8080/health.

### Demo accounts

The demo accounts are only created when demo mode is on, which it is in Development and in Docker Compose. Their passwords are public, so never turn demo mode on in a real deployment.

| Role   | Email                         | Password         |
|--------|-------------------------------|------------------|
| Admin  | `demo.admin@apartments.local` | `Demo#Admin2026` |
| Tenant | `juan.delacruz@example.com`   | `Tenant#2026x`   |

There are three more tenants with the same password: `maria.santos@`, `carlo.reyes@` and `ana.villanueva@example.com`.

## API overview

| Area        | Routes                                                                                                   | Who            |
|-------------|----------------------------------------------------------------------------------------------------------|----------------|
| Auth        | `POST /api/auth/login`, `refresh`, `logout`, `register-tenant`                                           | Login is public |
| Apartments  | `/api/apartments` (CRUD), `GET /api/apartments/available`                                                | Admin; vacant list is public |
| Tenants     | `/api/tenant` (CRUD)                                                                                     | Admin          |
| Leases      | `/api/lease`, `GET /api/lease/mine`, `POST /api/lease/{id}/terminate`                                    | Admin; tenants see their own |
| Billing     | `GET /api/billing/leases/{id}/charges`, `.../balance`, `GET /api/billing/charges/overdue`, `POST /api/billing/charges/{id}/payments`, `POST /api/billing/run` | Admin; tenants see their own |
| Maintenance | `/api/maintenance`, `GET /api/maintenance/mine`, `PATCH /api/maintenance/{id}/status`, `POST /api/maintenance/{id}/cancel` | Tenants report, admin manages |
| Reports     | `GET /api/reports/summary`                                                                               | Admin          |
| Health      | `GET /health`                                                                                            | Public         |

Full request and response details are in Swagger.

## Tests

```bash
dotnet test ApartmentManagementSystem.Tests
```

There are 106 tests. The unit tests run the services against the EF Core in-memory database. The integration tests start the whole API and call the real endpoints with real tokens. CI runs all of them on every push and pull request to `main`.

## Project structure

```
ApartmentManagementSystem/
  Controllers/    HTTP endpoints (no database access here)
  Services/       business rules and data access
  Interfaces/     service interfaces
  Models/         EF Core entities
  DTOs/           request and response shapes
  Validators/     FluentValidation rules
  Security/       roles and current-user access checks
  Data/           DbContext, seed data, demo accounts
  Migrations/     EF Core migrations
ApartmentManagementSystem.Tests/
  Integration/    end-to-end API tests
```
