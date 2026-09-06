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
            migrationBuilder.DropForeignKey(
                name: "FK_tenant_memberships_users_user_id",
                table: "tenant_memberships");

            migrationBuilder.DropIndex(
                name: "IX_tenant_memberships_tenant_id_user_id_role",
                table: "tenant_memberships");

            migrationBuilder.DropIndex(
                name: "IX_tenant_memberships_user_id",
                table: "tenant_memberships");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "users",
                type: "character varying(14)",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "text",
                nullable: true,
                computedColumnSql: "lower(email)",
                stored: true);

            // Upgrade the actual InitialSchema membership rows without choosing a role or
            // Tenant for ambiguous accounts. Existing synthetic Users retain access.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (
                        SELECT u.id FROM users u LEFT JOIN tenant_memberships m ON m.user_id = u.id
                        GROUP BY u.id HAVING count(m.id) <> 1
                    ) THEN
                        RAISE EXCEPTION 'Each existing User must have exactly one Tenant Membership before upgrading User management.';
                    END IF;
                END $$;
                UPDATE users u SET tenant_id = m.tenant_id, is_active = true
                FROM tenant_memberships m WHERE m.user_id = u.id;
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_users_id_tenant_id",
                table: "users",
                columns: new[] { "id", "tenant_id" });

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
                    Version = table.Column<int>(type: "integer", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "text", nullable: true, computedColumnSql: "lower(\"Email\")", stored: true)
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

            migrationBuilder.CreateTable(
                name: "user_access_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_access_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_access_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invitation_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitation_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invitation_history_invitations_invitation_id",
                        column: x => x.invitation_id,
                        principalTable: "invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_id",
                table: "users",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_tenant_id",
                table: "tenant_memberships",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_user_id",
                table: "tenant_memberships",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_user_id_tenant_id",
                table: "tenant_memberships",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_invitation_history_invitation_id",
                table: "invitation_history",
                column: "invitation_id");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_NormalizedEmail",
                table: "invitations",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_TenantId",
                table: "invitations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_user_access_history_user_id",
                table: "user_access_history",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_memberships_users_user_id_tenant_id",
                table: "tenant_memberships",
                columns: new[] { "user_id", "tenant_id" },
                principalTable: "users",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_tenants_tenant_id",
                table: "users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tenant_memberships_users_user_id_tenant_id",
                table: "tenant_memberships");

            migrationBuilder.DropForeignKey(
                name: "FK_users_tenants_tenant_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "invitation_history");

            migrationBuilder.DropTable(
                name: "user_access_history");

            migrationBuilder.DropTable(
                name: "invitations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_users_id_tenant_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_normalized_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_tenant_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_tenant_memberships_tenant_id",
                table: "tenant_memberships");

            migrationBuilder.DropIndex(
                name: "IX_tenant_memberships_user_id",
                table: "tenant_memberships");

            migrationBuilder.DropIndex(
                name: "IX_tenant_memberships_user_id_tenant_id",
                table: "tenant_memberships");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "version",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_tenant_id_user_id_role",
                table: "tenant_memberships",
                columns: new[] { "tenant_id", "user_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_user_id",
                table: "tenant_memberships",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_memberships_users_user_id",
                table: "tenant_memberships",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
