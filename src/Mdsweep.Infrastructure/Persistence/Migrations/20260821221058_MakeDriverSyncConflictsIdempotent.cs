using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeDriverSyncConflictsIdempotent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActionId",
                table: "DriverTripSyncConflicts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "DriverTripSyncConflicts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutcomeReason",
                table: "DriverTripSyncConflicts",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TripLogSigned",
                table: "DriverTripSyncConflicts",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripSyncConflicts_ProviderId_DriverId_ActionId",
                table: "DriverTripSyncConflicts",
                columns: new[] { "ProviderId", "DriverId", "ActionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DriverTripSyncConflicts_ProviderId_DriverId_ActionId",
                table: "DriverTripSyncConflicts");

            migrationBuilder.DropColumn(
                name: "ActionId",
                table: "DriverTripSyncConflicts");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "DriverTripSyncConflicts");

            migrationBuilder.DropColumn(
                name: "OutcomeReason",
                table: "DriverTripSyncConflicts");

            migrationBuilder.DropColumn(
                name: "TripLogSigned",
                table: "DriverTripSyncConflicts");
        }
    }
}
