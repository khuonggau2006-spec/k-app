using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMgmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_DueDateUtc",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_Status",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_IsActive_DueDateUtc_Status",
                table: "WorkTasks",
                columns: new[] { "IsActive", "DueDateUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_IsActive_Status_LocationId",
                table: "WorkTasks",
                columns: new[] { "IsActive", "Status", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAtUtc",
                table: "RefreshTokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAtUtc",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_IsActive_DueDateUtc_Status",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_IsActive_Status_LocationId",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ExpiresAtUtc",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAtUtc",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_DueDateUtc",
                table: "WorkTasks",
                column: "DueDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_Status",
                table: "WorkTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });
        }
    }
}
