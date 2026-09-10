using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE tenant_memberships RENAME COLUMN role TO roles;
                ALTER TABLE tenant_memberships ALTER COLUMN roles TYPE text[] USING ARRAY[roles]::text[];
                ALTER TABLE invitations RENAME COLUMN "Role" TO "Roles";
                ALTER TABLE invitations ALTER COLUMN "Roles" TYPE text[] USING ARRAY["Roles"]::text[];
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_memberships_roles",
                table: "tenant_memberships",
                sql: "cardinality(roles) BETWEEN 1 AND 2 AND roles <@ ARRAY['Administrator','Dispatcher','Driver']::text[] AND array_position(roles, NULL) IS NULL AND (cardinality(roles) = 1 OR roles[1] <> roles[2])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM tenant_memberships WHERE cardinality(roles) > 1)
                        OR EXISTS (SELECT 1 FROM invitations WHERE cardinality("Roles") > 1) THEN
                        RAISE EXCEPTION 'Cannot downgrade while Users or Invitations have two roles.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_memberships_roles",
                table: "tenant_memberships");

            migrationBuilder.Sql("""
                ALTER TABLE tenant_memberships ALTER COLUMN roles TYPE character varying(40) USING roles[1];
                ALTER TABLE tenant_memberships RENAME COLUMN roles TO role;
                ALTER TABLE invitations ALTER COLUMN "Roles" TYPE character varying(40) USING "Roles"[1];
                ALTER TABLE invitations RENAME COLUMN "Roles" TO "Role";
                """);
        }
    }
}
