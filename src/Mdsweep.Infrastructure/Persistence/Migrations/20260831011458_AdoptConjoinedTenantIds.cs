using System;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdoptConjoinedTenantIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Passengers_Tenants_TenantId",
                table: "Passengers");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMemberships_Tenants_TenantId",
                table: "TenantMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Passengers_TenantId_BrokerMemberId",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Passengers");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Passengers",
                newName: "tenant_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE "Tenants"
                ALTER COLUMN "Id" TYPE character varying(14)
                USING "Id"::text;

                ALTER TABLE "TenantMemberships"
                ALTER COLUMN "TenantId" TYPE character varying(14)
                USING "TenantId"::text;

                ALTER TABLE "Passengers"
                ALTER COLUMN "tenant_id" DROP DEFAULT,
                ALTER COLUMN "tenant_id" DROP NOT NULL,
                ALTER COLUMN "tenant_id" TYPE text
                USING "tenant_id"::text,
                ALTER COLUMN "tenant_id" SET DEFAULT '*DEFAULT*';
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_tenant_id",
                table: "Passengers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_tenant_id_BrokerMemberId",
                table: "Passengers",
                columns: new[] { "tenant_id", "BrokerMemberId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMemberships_Tenants_TenantId",
                table: "TenantMemberships",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantMemberships_Tenants_TenantId",
                table: "TenantMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Passengers_tenant_id",
                table: "Passengers");

            migrationBuilder.DropIndex(
                name: "IX_Passengers_tenant_id_BrokerMemberId",
                table: "Passengers");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "Passengers",
                newName: "TenantId");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Tenants",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(14)",
                oldMaxLength: 14);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "TenantMemberships",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(14)",
                oldMaxLength: 14);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Passengers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldDefaultValue: "*DEFAULT*");

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

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_TenantId_BrokerMemberId",
                table: "Passengers",
                columns: new[] { "TenantId", "BrokerMemberId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Passengers_Tenants_TenantId",
                table: "Passengers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMemberships_Tenants_TenantId",
                table: "TenantMemberships",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
