using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMgmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTaskReminderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueSoonReminderSentAtUtc",
                table: "WorkTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OverdueReminderSentAtUtc",
                table: "WorkTasks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueSoonReminderSentAtUtc",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "OverdueReminderSentAtUtc",
                table: "WorkTasks");
        }
    }
}
