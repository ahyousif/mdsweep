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
                name: "Passengers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: true),
                    BrokerMemberId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passengers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KeycloakOrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trip_imports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AppliedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_imports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    PassengerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerTripNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    appointment_time = table.Column<LocalTime>(type: "time", nullable: true),
                    pickup_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    pickup_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dropoff_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    dropoff_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    broker_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_will_call = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduledPickupTime = table.Column<LocalTime>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KeycloakUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trip_import_rows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    TripNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BrokerMemberId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AppointmentTime = table.Column<LocalTime>(type: "time", nullable: true),
                    PickupAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PickupCity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DropoffAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DropoffCity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BrokerStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsWillCall = table.Column<bool>(type: "boolean", nullable: false),
                    Disposition = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MessagesJson = table.Column<string>(type: "jsonb", nullable: false),
                    AppliedTripId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_import_rows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trip_import_rows_trip_imports_TripImportId",
                        column: x => x.TripImportId,
                        principalTable: "trip_imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_tenant_id_BrokerMemberId",
                table: "Passengers",
                columns: new[] { "tenant_id", "BrokerMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_TenantId_UserId",
                table: "TenantMemberships",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_UserId",
                table: "TenantMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_KeycloakOrganizationId",
                table: "Tenants",
                column: "KeycloakOrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trip_import_rows_TripImportId",
                table: "trip_import_rows",
                column: "TripImportId");

            migrationBuilder.CreateIndex(
                name: "IX_trip_imports_tenant_id_ContentFingerprint",
                table: "trip_imports",
                columns: new[] { "tenant_id", "ContentFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trips_tenant_id_BrokerTripNumber",
                table: "trips",
                columns: new[] { "tenant_id", "BrokerTripNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_KeycloakUserId",
                table: "Users",
                column: "KeycloakUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Passengers");

            migrationBuilder.DropTable(
                name: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "trip_import_rows");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "trip_imports");
        }
    }
}
