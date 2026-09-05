using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260904130000_AddTripMobilityRequirement")]
public partial class AddTripMobilityRequirement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "trip_import_receipts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                total = table.Column<int>(type: "integer", nullable: false),
                added = table.Column<int>(type: "integer", nullable: false),
                updated = table.Column<int>(type: "integer", nullable: false),
                unchanged = table.Column<int>(type: "integer", nullable: false),
                problem_count = table.Column<int>(type: "integer", nullable: false),
                imported_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_trip_import_receipts", x => x.id));

        migrationBuilder.AddColumn<string>(
            name: "mobility_requirement",
            table: "trips",
            type: "character varying(60)",
            maxLength: 60,
            nullable: false,
            defaultValue: "Unknown"
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
        migrationBuilder.DropTable(name: "trip_import_receipts");
        migrationBuilder.DropColumn(name: "mobility_requirement", table: "trips");
        migrationBuilder.DropColumn(name: "raw_imported_passenger_type", table: "trips");
        migrationBuilder.DropColumn(name: "trip_cost", table: "trips");
        migrationBuilder.DropColumn(name: "trip_mileage", table: "trips");
    }
}
