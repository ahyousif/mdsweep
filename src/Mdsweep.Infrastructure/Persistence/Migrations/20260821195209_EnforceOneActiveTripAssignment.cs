
#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOneActiveTripAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripAssignments_TripId_SupersededAt",
                table: "TripAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TripAssignments_TripId",
                table: "TripAssignments",
                column: "TripId",
                unique: true,
                filter: "\"SupersededAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripAssignments_TripId",
                table: "TripAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TripAssignments_TripId_SupersededAt",
                table: "TripAssignments",
                columns: new[] { "TripId", "SupersededAt" });
        }
    }
}
