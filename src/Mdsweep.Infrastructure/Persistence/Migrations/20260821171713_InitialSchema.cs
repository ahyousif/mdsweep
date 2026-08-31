using System;

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
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeycloakSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManifestPreviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KeycloakOrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "ProviderMemberships",
                columns: table => new
                {
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderMemberships", x => new { x.ProviderId, x.AppUserId });
                    table.ForeignKey(
                        name: "FK_ProviderMemberships_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderMemberships_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
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
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_KeycloakSubject",
                table: "AppUsers",
                column: "KeycloakSubject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManifestPreviews_ProviderId",
                table: "ManifestPreviews",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderMemberships_AppUserId",
                table: "ProviderMemberships",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_KeycloakOrganizationId",
                table: "Providers",
                column: "KeycloakOrganizationId",
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
                name: "IX_TripBrokerImports_ManifestPreviewId",
                table: "TripBrokerImports",
                column: "ManifestPreviewId");

            migrationBuilder.CreateIndex(
                name: "IX_TripBrokerImports_ProviderId_ManifestPreviewId_TripNumber",
                table: "TripBrokerImports",
                columns: new[] { "ProviderId", "ManifestPreviewId", "TripNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripBrokerImports_TripId",
                table: "TripBrokerImports",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ProviderId",
                table: "Trips",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ProviderId_TripNumber",
                table: "Trips",
                columns: new[] { "ProviderId", "TripNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderMemberships");

            migrationBuilder.DropTable(
                name: "ScheduledPickupTimeChanges");

            migrationBuilder.DropTable(
                name: "TripBrokerImports");

            migrationBuilder.DropTable(
                name: "TripSchedules");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "ManifestPreviews");

            migrationBuilder.DropTable(
                name: "Trips");
        }
    }
}
