using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MtmDriverNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManifestPreviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    RowsJson = table.Column<string>(type: "jsonb", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestPreviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Passengers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: true, defaultValue: "*DEFAULT*"),
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
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    TripNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JourneyKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AppointmentTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    MemberFirstName = table.Column<string>(type: "text", nullable: false),
                    MemberLastName = table.Column<string>(type: "text", nullable: false),
                    PickupAddress = table.Column<string>(type: "text", nullable: false),
                    PickupCity = table.Column<string>(type: "text", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: false),
                    DeliveryCity = table.Column<string>(type: "text", nullable: false),
                    PassengerPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PassengerType = table.Column<string>(type: "text", nullable: false),
                    VehicleType = table.Column<string>(type: "text", nullable: false),
                    BrokerStatus = table.Column<string>(type: "text", nullable: false),
                    IsWillCall = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
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
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Vin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverTripSyncConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeviceCapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TripLogSigned = table.Column<bool>(type: "boolean", nullable: true),
                    OutcomeReason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverTripSyncConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverTripSyncConflicts_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverTripEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeviceCapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OutcomeReason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TripLogSigned = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverTripEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverTripEvents_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverTripEvents_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledPickupTimeChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledPickupTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledPickupTimeChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledPickupTimeChanges_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripBrokerImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManifestPreviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TripNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AppointmentTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    PickupAddress = table.Column<string>(type: "text", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: false),
                    BrokerStatus = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripBrokerImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripBrokerImports_ManifestPreviews_ManifestPreviewId",
                        column: x => x.ManifestPreviewId,
                        principalTable: "ManifestPreviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripBrokerImports_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripSchedules",
                columns: table => new
                {
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledPickupTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripSchedules", x => x.TripId);
                    table.ForeignKey(
                        name: "FK_TripSchedules_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "TripAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SupersededAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripAssignments_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripAssignments_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverTripEventCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverTripEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectedByDriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectedDeviceCapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverTripEventCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverTripEventCorrections_DriverTripEvents_DriverTripEvent~",
                        column: x => x.DriverTripEventId,
                        principalTable: "DriverTripEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverTripEventCorrections_Drivers_CorrectedByDriverId",
                        column: x => x.CorrectedByDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_TenantId_MtmDriverNumber",
                table: "Drivers",
                columns: new[] { "TenantId", "MtmDriverNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_TenantId_UserId",
                table: "Drivers",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripEventCorrections_CorrectedByDriverId",
                table: "DriverTripEventCorrections",
                column: "CorrectedByDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripEventCorrections_DriverTripEventId_ReceivedAt",
                table: "DriverTripEventCorrections",
                columns: new[] { "DriverTripEventId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripEvents_DriverId",
                table: "DriverTripEvents",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripEvents_TripId_DeviceCapturedAt",
                table: "DriverTripEvents",
                columns: new[] { "TripId", "DeviceCapturedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripEvents_TripId_ReceivedAt",
                table: "DriverTripEvents",
                columns: new[] { "TripId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripSyncConflicts_DriverId",
                table: "DriverTripSyncConflicts",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripSyncConflicts_TenantId_DriverId_ActionId",
                table: "DriverTripSyncConflicts",
                columns: new[] { "TenantId", "DriverId", "ActionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverTripSyncConflicts_TenantId_ReceivedAt",
                table: "DriverTripSyncConflicts",
                columns: new[] { "TenantId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ManifestPreviews_TenantId",
                table: "ManifestPreviews",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_tenant_id",
                table: "Passengers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_tenant_id_BrokerMemberId",
                table: "Passengers",
                columns: new[] { "tenant_id", "BrokerMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPickupTimeChanges_Sequence",
                table: "ScheduledPickupTimeChanges",
                column: "Sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPickupTimeChanges_TripId_ChangedAt",
                table: "ScheduledPickupTimeChanges",
                columns: new[] { "TripId", "ChangedAt" });

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
                name: "IX_TripAssignments_DriverId",
                table: "TripAssignments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_TripAssignments_TripId",
                table: "TripAssignments",
                column: "TripId",
                unique: true,
                filter: "\"SupersededAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TripAssignments_VehicleId",
                table: "TripAssignments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TripBrokerImports_ManifestPreviewId",
                table: "TripBrokerImports",
                column: "ManifestPreviewId");

            migrationBuilder.CreateIndex(
                name: "IX_TripBrokerImports_TenantId_ManifestPreviewId_TripNumber",
                table: "TripBrokerImports",
                columns: new[] { "TenantId", "ManifestPreviewId", "TripNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripBrokerImports_TripId",
                table: "TripBrokerImports",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TenantId",
                table: "Trips",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TenantId_TripNumber",
                table: "Trips",
                columns: new[] { "TenantId", "TripNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_KeycloakUserId",
                table: "Users",
                column: "KeycloakUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TenantId_Vin",
                table: "Vehicles",
                columns: new[] { "TenantId", "Vin" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverTripEventCorrections");

            migrationBuilder.DropTable(
                name: "DriverTripSyncConflicts");

            migrationBuilder.DropTable(
                name: "Passengers");

            migrationBuilder.DropTable(
                name: "ScheduledPickupTimeChanges");

            migrationBuilder.DropTable(
                name: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "TripAssignments");

            migrationBuilder.DropTable(
                name: "TripBrokerImports");

            migrationBuilder.DropTable(
                name: "TripSchedules");

            migrationBuilder.DropTable(
                name: "DriverTripEvents");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "ManifestPreviews");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "Trips");
        }
    }
}
