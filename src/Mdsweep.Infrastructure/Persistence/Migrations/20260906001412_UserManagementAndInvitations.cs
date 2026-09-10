using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserManagementAndInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_memberships_tenant_id_user_id_role",
                table: "tenant_memberships");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "tenant_memberships",
                type: "character varying(401)",
                maxLength: 401,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "tenant_memberships",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "tenant_memberships",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    DeliveryError = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AcceptedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invitations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_tenant_id_user_id",
                table: "tenant_memberships",
                columns: new[] { "tenant_id", "user_id" },
                unique: true);


            migrationBuilder.CreateIndex(
                name: "IX_invitations_TenantId_Email",
                table: "invitations",
                columns: new[] { "TenantId", "Email" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_TenantId",
                table: "invitations",
                column: "TenantId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "invitations");
            migrationBuilder.DropIndex(name: "IX_users_email", table: "users");
            migrationBuilder.DropIndex(name: "IX_tenant_memberships_tenant_id_user_id", table: "tenant_memberships");
            migrationBuilder.DropColumn(name: "email", table: "users");
            migrationBuilder.DropColumn(name: "display_name", table: "tenant_memberships");
            migrationBuilder.DropColumn(name: "is_active", table: "tenant_memberships");
            migrationBuilder.DropColumn(name: "version", table: "tenant_memberships");
            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_tenant_id_user_id_role",
                table: "tenant_memberships",
                columns: new[] { "tenant_id", "user_id", "role" },
                unique: true);
        }
    }
}
