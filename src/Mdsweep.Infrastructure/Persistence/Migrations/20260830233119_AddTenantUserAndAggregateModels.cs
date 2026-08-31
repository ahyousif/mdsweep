using System;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantUserAndAggregateModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Passengers_ProviderId_BrokerMemberId",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "BrokerMemberId",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Passengers");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Passengers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Passengers");

            migrationBuilder.AddColumn<string>(
                name: "BrokerMemberId",
                table: "Passengers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Passengers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Passengers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "Passengers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_ProviderId_BrokerMemberId",
                table: "Passengers",
                columns: new[] { "ProviderId", "BrokerMemberId" });
        }
    }
}
