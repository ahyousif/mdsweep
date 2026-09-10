using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations;

public partial class UserManagementInvitationTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_tenant_memberships_tenant_id_user_id_role",
            table: "tenant_memberships"
        );

        migrationBuilder.AddColumn<string>(
            name: "email",
            table: "users",
            type: "character varying(254)",
            maxLength: 254,
            nullable: false,
            defaultValue: ""
        );
        migrationBuilder.AddColumn<string>(
            name: "display_name",
            table: "tenant_memberships",
            type: "character varying(401)",
            maxLength: 401,
            nullable: true
        );
        migrationBuilder.AddColumn<bool>(
            name: "is_active",
            table: "tenant_memberships",
            type: "boolean",
            nullable: false,
            defaultValue: true
        );
        migrationBuilder.AddColumn<string[]>(
            name: "roles",
            table: "tenant_memberships",
            type: "text[]",
            nullable: true
        );
        migrationBuilder.AddColumn<int>(
            name: "version",
            table: "tenant_memberships",
            type: "integer",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.Sql("UPDATE tenant_memberships SET roles = ARRAY[role];");
        migrationBuilder.AlterColumn<string[]>(
            name: "roles",
            table: "tenant_memberships",
            type: "text[]",
            nullable: false,
            oldClrType: typeof(string[]),
            oldType: "text[]",
            oldNullable: true
        );
        migrationBuilder.DropColumn(name: "role", table: "tenant_memberships");

        migrationBuilder.CreateTable(
            name: "invitations",
            columns: table =>
                new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(
                        type: "character varying(14)",
                        maxLength: 14,
                        nullable: false
                    ),
                    email = table.Column<string>(
                        type: "character varying(254)",
                        maxLength: 254,
                        nullable: false
                    ),
                    first_name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    last_name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    roles = table.Column<string[]>(type: "text[]", nullable: false),
                    token_hash = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    expires_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    delivery_error = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: true
                    ),
                    accepted_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                },
            constraints: table =>
            {
                table.PrimaryKey("pk_invitations", x => x.id);
                table.ForeignKey(
                    name: "fk_invitations_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(name: "ix_users_email", table: "users", column: "email", unique: true);
        migrationBuilder.CreateIndex(
            name: "ix_tenant_memberships_tenant_id_user_id",
            table: "tenant_memberships",
            columns: new[] { "tenant_id", "user_id" },
            unique: true
        );
        migrationBuilder.AddCheckConstraint(
            name: "ck_tenant_memberships_roles",
            table: "tenant_memberships",
            sql: "cardinality(roles) BETWEEN 1 AND 2 AND roles <@ ARRAY['Administrator','Dispatcher','Driver']::text[] AND array_position(roles, NULL) IS NULL AND (cardinality(roles) = 1 OR roles[1] <> roles[2])"
        );
        migrationBuilder.CreateIndex(
            name: "ix_invitations_tenant_id",
            table: "invitations",
            column: "tenant_id"
        );
        migrationBuilder.CreateIndex(
            name: "ix_invitations_tenant_id_email",
            table: "invitations",
            columns: new[] { "tenant_id", "email" },
            unique: true,
            filter: "status = 'Pending'"
        );
        migrationBuilder.CreateIndex(
            name: "ix_invitations_token_hash",
            table: "invitations",
            column: "token_hash",
            unique: true
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "invitations");
        migrationBuilder.DropIndex(name: "ix_users_email", table: "users");
        migrationBuilder.DropIndex(name: "ix_tenant_memberships_tenant_id_user_id", table: "tenant_memberships");
        migrationBuilder.DropCheckConstraint(name: "ck_tenant_memberships_roles", table: "tenant_memberships");

        migrationBuilder.AddColumn<string>(
            name: "role",
            table: "tenant_memberships",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true
        );
        migrationBuilder.Sql("UPDATE tenant_memberships SET role = roles[1];");
        migrationBuilder.AlterColumn<string>(
            name: "role",
            table: "tenant_memberships",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(40)",
            oldMaxLength: 40,
            oldNullable: true
        );

        migrationBuilder.DropColumn(name: "email", table: "users");
        migrationBuilder.DropColumn(name: "display_name", table: "tenant_memberships");
        migrationBuilder.DropColumn(name: "is_active", table: "tenant_memberships");
        migrationBuilder.DropColumn(name: "roles", table: "tenant_memberships");
        migrationBuilder.DropColumn(name: "version", table: "tenant_memberships");
        migrationBuilder.CreateIndex(
            name: "IX_tenant_memberships_tenant_id_user_id_role",
            table: "tenant_memberships",
            columns: new[] { "tenant_id", "user_id", "role" },
            unique: true
        );
    }
}
