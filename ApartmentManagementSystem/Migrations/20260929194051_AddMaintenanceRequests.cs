using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApartmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApartmentId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_Apartments_ApartmentId",
                        column: x => x.ApartmentId,
                        principalTable: "Apartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MaintenanceRequests",
                columns: new[] { "Id", "ApartmentId", "CreatedAt", "Description", "Priority", "ResolutionNotes", "ResolvedAt", "Status", "TenantId", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1001, 1001, new DateTime(2026, 9, 20, 8, 30, 0, 0, DateTimeKind.Unspecified), "The kitchen faucet drips constantly, even when fully closed.", 1, null, null, "Open", 1001, "Leaking kitchen faucet", new DateTime(2026, 9, 20, 8, 30, 0, 0, DateTimeKind.Unspecified) },
                    { 1002, 1002, new DateTime(2026, 9, 15, 14, 0, 0, 0, DateTimeKind.Unspecified), "The bedroom aircon runs but only blows warm air.", 2, null, null, "InProgress", 1002, "Aircon not cooling", new DateTime(2026, 9, 16, 9, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 1003, 1004, new DateTime(2026, 9, 28, 19, 45, 0, 0, DateTimeKind.Unspecified), "All outlets in the living room stopped working after a brownout.", 3, null, null, "Open", 1004, "No power in living room outlets", new DateTime(2026, 9, 28, 19, 45, 0, 0, DateTimeKind.Unspecified) },
                    { 1004, 1001, new DateTime(2026, 8, 3, 10, 0, 0, 0, DateTimeKind.Unspecified), "Main door lock is hard to turn and sometimes jams.", 2, "Replaced the lock cylinder and gave the tenant new keys.", new DateTime(2026, 8, 4, 16, 0, 0, 0, DateTimeKind.Unspecified), "Resolved", 1001, "Broken door lock", new DateTime(2026, 8, 4, 16, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 1005, 1005, new DateTime(2026, 9, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), "Walls in the bedroom need repainting before the unit is shown.", 0, null, null, "Open", null, "Repaint before next tenant", new DateTime(2026, 9, 10, 9, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_ApartmentId",
                table: "MaintenanceRequests",
                column: "ApartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_Status_Priority",
                table: "MaintenanceRequests",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_TenantId",
                table: "MaintenanceRequests",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceRequests");
        }
    }
}
