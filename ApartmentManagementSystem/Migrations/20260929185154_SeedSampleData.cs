using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApartmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class SeedSampleData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Apartments",
                columns: new[] { "Id", "Address", "HasParking", "Location", "Name", "Rent", "Rooms" },
                values: new object[,]
                {
                    { 1001, "123 Ayala Ave", true, "Makati", "Sunset Residences 3B", 25000m, 2 },
                    { 1002, "45 Katipunan Ave", false, "Quezon City", "Greenview Tower 12A", 18000m, 1 },
                    { 1003, "88 Roxas Blvd", true, "Pasay", "Bayside Lofts 7C", 22000m, 2 },
                    { 1004, "10 BGC 5th Ave", true, "Taguig", "Palm Court 2F", 35000m, 3 },
                    { 1005, "7 Shaw Blvd", false, "Mandaluyong", "Maple Homes 1A", 15000m, 1 }
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "ApartmentId", "Email", "MoveInDate", "MoveOutDate", "Name", "PermanentAddress", "PhoneNumber" },
                values: new object[,]
                {
                    { 1001, 1001, "juan.delacruz@example.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Juan Dela Cruz", "12 Rizal St, Cebu City", "09171234567" },
                    { 1002, 1002, "maria.santos@example.com", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Maria Santos", "34 Bonifacio St, Davao City", "09181234567" },
                    { 1003, 1003, "carlo.reyes@example.com", new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carlo Reyes", "56 Mabini St, Iloilo City", "09191234567" },
                    { 1004, 1004, "ana.villanueva@example.com", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Ana Villanueva", "78 Luna St, Baguio City", "09201234567" }
                });

            migrationBuilder.InsertData(
                table: "Leases",
                columns: new[] { "Id", "ApartmentId", "EndDate", "MonthlyRent", "SecurityDeposit", "StartDate", "Status", "TenantId", "TerminatedDate" },
                values: new object[,]
                {
                    { 1001, 1001, new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 25000m, 50000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Active", 1001, null },
                    { 1002, 1002, new DateTime(2027, 2, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 17500m, 35000m, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Active", 1002, null },
                    { 1003, 1003, new DateTime(2026, 5, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 22000m, 44000m, new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Terminated", 1003, new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 1004, 1004, new DateTime(2027, 4, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 35000m, 70000m, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Active", 1004, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Apartments",
                keyColumn: "Id",
                keyValue: 1005);

            migrationBuilder.DeleteData(
                table: "Leases",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Leases",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Leases",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "Leases",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "Apartments",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Apartments",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Apartments",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "Apartments",
                keyColumn: "Id",
                keyValue: 1004);
        }
    }
}
