using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mdsweep.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverOfflineReliability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverTripEventCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverTripEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectedByDriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectedDeviceCapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverTripEventCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverTripEventCorrections_DriverTripEvents_DriverTripEvent~",
                        column: x => x.DriverTripEventId,
                        principalTable: "DriverTripEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverTripEventCorrections_Drivers_CorrectedByDriverId",
                        column: x => x.CorrectedByDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverTripSyncConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeviceCapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverTripSyncConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverTripSyncConflicts_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripEventCorrections_CorrectedByDriverId",
                table: "DriverTripEventCorrections",
                column: "CorrectedByDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripEventCorrections_DriverTripEventId_ReceivedAt",
                table: "DriverTripEventCorrections",
                columns: new[] { "DriverTripEventId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripSyncConflicts_DriverId",
                table: "DriverTripSyncConflicts",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripSyncConflicts_ProviderId_ReceivedAt",
                table: "DriverTripSyncConflicts",
                columns: new[] { "ProviderId", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverTripEventCorrections");

            migrationBuilder.DropTable(
                name: "DriverTripSyncConflicts");
        }
    }
}
