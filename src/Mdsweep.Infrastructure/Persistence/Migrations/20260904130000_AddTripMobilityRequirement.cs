using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260904130000_AddTripMobilityRequirement")]
public partial class AddTripMobilityRequirement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "mobility_requirement",
            table: "trips",
            type: "character varying(60)",
            maxLength: 60,
            nullable: false,
            defaultValue: "Ambulatory"
        );

        migrationBuilder.AddColumn<string>(
            name: "raw_imported_passenger_type",
            table: "trips",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true
        );

        migrationBuilder.AddColumn<decimal>(
            name: "trip_cost",
            table: "trips",
            type: "numeric(10,2)",
            precision: 10,
            scale: 2,
            nullable: true
        );

        migrationBuilder.AddColumn<decimal>(
            name: "trip_mileage",
            table: "trips",
            type: "numeric(10,2)",
            precision: 10,
            scale: 2,
            nullable: true
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "mobility_requirement", table: "trips");
        migrationBuilder.DropColumn(name: "raw_imported_passenger_type", table: "trips");
        migrationBuilder.DropColumn(name: "trip_cost", table: "trips");
        migrationBuilder.DropColumn(name: "trip_mileage", table: "trips");
    }
}
