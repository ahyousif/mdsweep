using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Mdsweep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "passengers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    broker_member_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passengers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    keycloak_organization_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pickup_buffer_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 15)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    keycloak_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    broker_trip_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    service_date = table.Column<LocalDate>(type: "date", nullable: false),
                    broker_time = table.Column<LocalTime>(type: "time", nullable: true),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    is_will_call = table.Column<bool>(type: "boolean", nullable: false),
                    pickup_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    pickup_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    pickup_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pickup_zip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    dropoff_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    dropoff_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dropoff_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dropoff_zip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    broker_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    passenger_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    special_needs = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    trip_cost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    trip_mileage = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    calculated_pickup_time = table.Column<LocalTime>(type: "time", nullable: true),
                    manual_pickup_time = table.Column<LocalTime>(type: "time", nullable: true),
                    estimated_travel_minutes = table.Column<int>(type: "integer", nullable: true),
                    estimated_distance_meters = table.Column<int>(type: "integer", nullable: true),
                    schedule_input_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.id);
                    table.ForeignKey(
                        name: "FK_trips_passengers_PassengerId",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_memberships_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_passengers_TenantId_BrokerMemberId",
                table: "passengers",
                columns: new[] { "tenant_id", "broker_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_tenant_id_user_id_role",
                table: "tenant_memberships",
                columns: new[] { "tenant_id", "user_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_user_id",
                table: "tenant_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_KeycloakOrganizationId",
                table: "tenants",
                column: "keycloak_organization_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trips_PassengerId",
                table: "trips",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_TenantId_BrokerTripNumber",
                table: "trips",
                columns: new[] { "tenant_id", "broker_trip_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_keycloak_user_id",
                table: "users",
                column: "keycloak_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_memberships");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "passengers");
        }
    }
}
