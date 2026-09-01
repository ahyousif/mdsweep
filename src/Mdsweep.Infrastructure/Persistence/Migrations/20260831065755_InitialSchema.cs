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
                    keycloak_organization_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trip_imports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    applied_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_imports", x => x.id);
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
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    appointment_time = table.Column<LocalTime>(type: "time", nullable: true),
                    pickup_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    pickup_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dropoff_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    dropoff_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    broker_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_will_call = table.Column<bool>(type: "boolean", nullable: false),
                    scheduled_pickup_time = table.Column<LocalTime>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.id);
                    table.ForeignKey(
                        name: "FK_trips_passengers_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "passengers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trip_import_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    trip_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    broker_member_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    service_date = table.Column<DateOnly>(type: "date", nullable: true),
                    appointment_time = table.Column<LocalTime>(type: "time", nullable: true),
                    pickup_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pickup_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dropoff_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    dropoff_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    broker_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_will_call = table.Column<bool>(type: "boolean", nullable: false),
                    disposition = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    applied_trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    messages = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_import_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_trip_import_items_trip_imports_trip_import_id",
                        column: x => x.trip_import_id,
                        principalTable: "trip_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_passengers_tenant_id_broker_member_id",
                table: "passengers",
                columns: new[] { "tenant_id", "broker_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_tenant_id_user_id",
                table: "tenant_memberships",
                columns: new[] { "tenant_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_user_id",
                table: "tenant_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_keycloak_organization_id",
                table: "tenants",
                column: "keycloak_organization_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trip_import_items_trip_import_id",
                table: "trip_import_items",
                column: "trip_import_id");

            migrationBuilder.CreateIndex(
                name: "IX_trip_imports_tenant_id_content_fingerprint",
                table: "trip_imports",
                columns: new[] { "tenant_id", "content_fingerprint" },
                unique: true,
                filter: "status = 'Applied'");

            migrationBuilder.CreateIndex(
                name: "IX_trips_passenger_id",
                table: "trips",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_tenant_id_broker_trip_number",
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
                name: "trip_import_items");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "trip_imports");

            migrationBuilder.DropTable(
                name: "passengers");
        }
    }
}
