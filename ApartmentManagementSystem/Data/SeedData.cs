using ApartmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Data
{
    // Sample data applied through migrations (HasData).
    // IDs start at 1001 so they don't collide with rows created manually through the API.
    // Values must be static (no DateTime.Now) or EF will generate a new migration every time.
    public static class SeedData
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Apartment>().HasData(
                new Apartment { Id = 1001, Name = "Sunset Residences 3B", Location = "Makati", Address = "123 Ayala Ave", Rent = 25000, Rooms = 2, HasParking = true },
                new Apartment { Id = 1002, Name = "Greenview Tower 12A", Location = "Quezon City", Address = "45 Katipunan Ave", Rent = 18000, Rooms = 1, HasParking = false },
                new Apartment { Id = 1003, Name = "Bayside Lofts 7C", Location = "Pasay", Address = "88 Roxas Blvd", Rent = 22000, Rooms = 2, HasParking = true },
                new Apartment { Id = 1004, Name = "Palm Court 2F", Location = "Taguig", Address = "10 BGC 5th Ave", Rent = 35000, Rooms = 3, HasParking = true },
                new Apartment { Id = 1005, Name = "Maple Homes 1A", Location = "Mandaluyong", Address = "7 Shaw Blvd", Rent = 15000, Rooms = 1, HasParking = false }
            );

            modelBuilder.Entity<Tenant>().HasData(
                new Tenant { Id = 1001, Name = "Juan Dela Cruz", Email = "juan.delacruz@example.com", PhoneNumber = "09171234567", PermanentAddress = "12 Rizal St, Cebu City", MoveInDate = new DateTime(2026, 1, 1), ApartmentId = 1001 },
                new Tenant { Id = 1002, Name = "Maria Santos", Email = "maria.santos@example.com", PhoneNumber = "09181234567", PermanentAddress = "34 Bonifacio St, Davao City", MoveInDate = new DateTime(2026, 3, 1), ApartmentId = 1002 },
                new Tenant { Id = 1003, Name = "Carlo Reyes", Email = "carlo.reyes@example.com", PhoneNumber = "09191234567", PermanentAddress = "56 Mabini St, Iloilo City", MoveInDate = new DateTime(2025, 6, 1), MoveOutDate = new DateTime(2026, 2, 28), ApartmentId = 1003 },
                new Tenant { Id = 1004, Name = "Ana Villanueva", Email = "ana.villanueva@example.com", PhoneNumber = "09201234567", PermanentAddress = "78 Luna St, Baguio City", MoveInDate = new DateTime(2026, 5, 1), ApartmentId = 1004 }
            );

            modelBuilder.Entity<Lease>().HasData(
                // Active leases: apartments 1001, 1002, 1004 are occupied
                new Lease { Id = 1001, TenantId = 1001, ApartmentId = 1001, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31), MonthlyRent = 25000, SecurityDeposit = 50000, Status = LeaseStatus.Active },
                new Lease { Id = 1002, TenantId = 1002, ApartmentId = 1002, StartDate = new DateTime(2026, 3, 1), EndDate = new DateTime(2027, 2, 28), MonthlyRent = 17500, SecurityDeposit = 35000, Status = LeaseStatus.Active },
                new Lease { Id = 1004, TenantId = 1004, ApartmentId = 1004, StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2027, 4, 30), MonthlyRent = 35000, SecurityDeposit = 70000, Status = LeaseStatus.Active },
                // Terminated early: apartment 1003 is available again
                new Lease { Id = 1003, TenantId = 1003, ApartmentId = 1003, StartDate = new DateTime(2025, 6, 1), EndDate = new DateTime(2026, 5, 31), TerminatedDate = new DateTime(2026, 2, 28), MonthlyRent = 22000, SecurityDeposit = 44000, Status = LeaseStatus.Terminated }
                // Apartment 1005 has never been leased
            );

            // One request in each status, so every part of the workflow can be tried
            modelBuilder.Entity<MaintenanceRequest>().HasData(
                new MaintenanceRequest { Id = 1001, ApartmentId = 1001, TenantId = 1001, Title = "Leaking kitchen faucet", Description = "The kitchen faucet drips constantly, even when fully closed.", Priority = MaintenancePriority.Normal, Status = MaintenanceStatus.Open, CreatedAt = new DateTime(2026, 9, 20, 8, 30, 0), UpdatedAt = new DateTime(2026, 9, 20, 8, 30, 0) },
                new MaintenanceRequest { Id = 1002, ApartmentId = 1002, TenantId = 1002, Title = "Aircon not cooling", Description = "The bedroom aircon runs but only blows warm air.", Priority = MaintenancePriority.High, Status = MaintenanceStatus.InProgress, CreatedAt = new DateTime(2026, 9, 15, 14, 0, 0), UpdatedAt = new DateTime(2026, 9, 16, 9, 0, 0) },
                new MaintenanceRequest { Id = 1003, ApartmentId = 1004, TenantId = 1004, Title = "No power in living room outlets", Description = "All outlets in the living room stopped working after a brownout.", Priority = MaintenancePriority.Urgent, Status = MaintenanceStatus.Open, CreatedAt = new DateTime(2026, 9, 28, 19, 45, 0), UpdatedAt = new DateTime(2026, 9, 28, 19, 45, 0) },
                new MaintenanceRequest { Id = 1004, ApartmentId = 1001, TenantId = 1001, Title = "Broken door lock", Description = "Main door lock is hard to turn and sometimes jams.", Priority = MaintenancePriority.High, Status = MaintenanceStatus.Resolved, CreatedAt = new DateTime(2026, 8, 3, 10, 0, 0), UpdatedAt = new DateTime(2026, 8, 4, 16, 0, 0), ResolvedAt = new DateTime(2026, 8, 4, 16, 0, 0), ResolutionNotes = "Replaced the lock cylinder and gave the tenant new keys." },
                // Logged by the admin for the vacant unit, so no tenant
                new MaintenanceRequest { Id = 1005, ApartmentId = 1005, TenantId = null, Title = "Repaint before next tenant", Description = "Walls in the bedroom need repainting before the unit is shown.", Priority = MaintenancePriority.Low, Status = MaintenanceStatus.Open, CreatedAt = new DateTime(2026, 9, 10, 9, 0, 0), UpdatedAt = new DateTime(2026, 9, 10, 9, 0, 0) }
            );
        }
    }
}
