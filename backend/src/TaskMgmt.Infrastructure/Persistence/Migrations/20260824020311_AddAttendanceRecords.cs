using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMgmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CheckInRadiusMeters",
                table: "Locations",
                type: "double precision",
                nullable: false,
                defaultValue: 100.0);

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckInAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CheckInLatitude = table.Column<double>(type: "double precision", nullable: true),
                    CheckInLongitude = table.Column<double>(type: "double precision", nullable: true),
                    CheckInLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckOutAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CheckOutLatitude = table.Column<double>(type: "double precision", nullable: true),
                    CheckOutLongitude = table.Column<double>(type: "double precision", nullable: true),
                    CheckOutLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Locations_CheckInLocationId",
                        column: x => x.CheckInLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Locations_CheckOutLocationId",
                        column: x => x.CheckOutLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_CheckInLocationId",
                table: "AttendanceRecords",
                column: "CheckInLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_CheckOutLocationId",
                table: "AttendanceRecords",
                column: "CheckOutLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_CreatedByUserId",
                table: "AttendanceRecords",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_UpdatedByUserId",
                table: "AttendanceRecords",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_UserId_WorkDate",
                table: "AttendanceRecords",
                columns: new[] { "UserId", "WorkDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInRadiusMeters",
                table: "Locations");
        }
    }
}
