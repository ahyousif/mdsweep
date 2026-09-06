using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260904120000_RemoveTripImportPreviewPersistence")]
public partial class RemoveTripImportPreviewPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "trip_import_items");
        migrationBuilder.DropTable(name: "trip_imports");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("The removed trip import preview data cannot be restored.");
    }
}
