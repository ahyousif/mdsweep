using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.DriverWork;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public abstract class MdsweepIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
    protected WebApplicationFactory<Program> Application = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        Application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:mdsweep", database.GetConnectionString());
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IKeycloakUserAdministration>();
                services.AddSingleton<IKeycloakUserAdministration, TestKeycloakUserAdministration>();
                services.RemoveAll<IDriverWorkClock>();
                services.AddSingleton<IDriverWorkClock>(
                    new TestDriverWorkClock(new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero))
                );
                services
                    .AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, DispatcherAuthenticationHandler>("Test", _ => { });
            });
        });
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = TenantAggregate.Create("mdsw-eep2-3456", "Synthetic Tenant", "synthetic-tenant");
        var user = UserAggregate.Create("Synthetic", "Dispatcher", "dispatcher-test");
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, user.Id, "Dispatcher"));
        await db.SaveChangesAsync();
        var tenants = scope.ServiceProvider.GetRequiredService<IDynamicTenantSource<string>>();
        await tenants.AddTenantAsync(tenant.Id, CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await Application.DisposeAsync();
        await database.DisposeAsync();
    }

    protected static string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    protected static async Task<PreviewResponse> Preview(HttpClient client, string fixture)
    {
        await AddAntiforgeryToken(client);
        await using var file = File.OpenRead(FixturePath(fixture));
        using var form = new MultipartFormDataContent { { new StreamContent(file), "file", fixture } };
        using var response = await client.PostAsync("/api/manifest-imports/preview", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PreviewResponse>())!;
    }

    protected static async Task<PreviewResponse> PreviewCsv(
        HttpClient client,
        string csv,
        string fileName = "manifest.csv"
    )
    {
        await AddAntiforgeryToken(client);
        using var form = new MultipartFormDataContent { { new StringContent(csv), "file", fileName } };
        using var response = await client.PostAsync("/api/manifest-imports/preview", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PreviewResponse>())!;
    }

    protected static async Task AddAntiforgeryToken(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<AntiforgeryResponse>("/api/auth/antiforgery");
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", result!.Token);
    }

    protected async Task<(Guid DriverId, Guid VehicleId)> AddActiveResources()
    {
        const string tenantId = "mdsw-eep2-3456";
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = UserAggregate.Create("Synthetic", "Driver", $"driver-{Guid.CreateVersion7()}");
        var driver = new Driver
        {
            TenantId = tenantId,
            UserId = user.Id,
            DisplayName = "Synthetic Driver",
            MtmDriverNumber = $"DRV-{Guid.CreateVersion7():N}",
        };
        var vehicle = new Vehicle
        {
            TenantId = tenantId,
            DisplayName = "Van",
            Vin = $"VIN{Guid.CreateVersion7():N}"[..17],
        };
        db.Users.Add(user);
        db.TenantMemberships.Add(TenantMembership.Create(tenantId, user.Id, "Driver"));
        db.Drivers.Add(driver);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return (driver.Id, vehicle.Id);
    }

    protected async Task AssignTripToAuthenticatedDriver(HttpClient client, string tripNumber)
    {
        const string tenantId = "mdsw-eep2-3456";
        Guid driverId;
        Guid vehicleId;
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync(x => x.KeycloakUserId == "dispatcher-test");
            var driver = new Driver
            {
                TenantId = tenantId,
                UserId = user.Id,
                DisplayName = "Authenticated Driver",
                MtmDriverNumber = $"DRV-{tripNumber}",
            };
            var vehicle = new Vehicle
            {
                TenantId = tenantId,
                DisplayName = "Driver Van",
                Vin = $"VIN{tripNumber}".PadRight(17, '0'),
            };
            db.Drivers.Add(driver);
            db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();
            driverId = driver.Id;
            vehicleId = vehicle.Id;
        }

        var preview = await PreviewCsv(
            client,
            Manifest(
                Row(tripNumber, "VALID", "0915", "100 First St", "200 Main St"),
                Row($"{tripNumber}OTHER", "VALID", "1015", "300 Second St", "400 Oak St")
            )
        );
        using var apply = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        apply.EnsureSuccessStatusCode();
        using var assignment = await client.PostAsJsonAsync(
            $"/api/trips/{tripNumber}/assignments",
            new { driverId, vehicleId }
        );
        assignment.EnsureSuccessStatusCode();

        await using var driverScope = Application.Services.CreateAsyncScope();
        var driverDb = driverScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await driverDb.TenantMemberships.SingleAsync(x => x.TenantId == tenantId);
        driverDb.TenantMemberships.Remove(membership);
        driverDb.TenantMemberships.Add(TenantMembership.Create(tenantId, membership.UserId, "Driver"));
        await driverDb.SaveChangesAsync();
    }

    protected static string Manifest(params string[] rows) =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Passenger Type,Vehicle Type,Will Call Flag\n"
        + string.Join('\n', rows);

    protected static string Row(string tripNumber, string status, string time, string pickup, string delivery) =>
        $"09/15/2026,{delivery},{pickup},{time},{tripNumber},{status},Test,Rider,Phoenix,Mesa,Ambulatory,Cab,N";

    protected sealed record PreviewResponse(
        Guid PreviewId,
        int Ready,
        int Warning,
        int Blocked,
        List<DateOnly> ServiceDates,
        List<PreviewRow> Rows
    );

    protected sealed record PreviewRow(
        string TripNumber,
        string Disposition,
        string BrokerChange,
        bool HasProviderOverrides,
        bool IsActive,
        IReadOnlyList<string> Messages
    );

    protected sealed record ApplyResponse(int Imported, int Blocked);

    protected sealed record AntiforgeryResponse(string Token);

    protected sealed record ServiceDayTrip(
        string TripNumber,
        string JourneyKey,
        string BrokerStatus,
        TimeOnly AppointmentTime,
        string PickupAddress,
        TimeOnly? ScheduledPickupTime,
        bool IsActive
    );

    protected sealed record ScheduledPickupChange(long Sequence, TimeOnly ScheduledPickupTime, string ChangedBy);

    protected sealed record AssignmentResponse(
        Guid DriverId,
        Guid VehicleId,
        Guid AssignedByUserId,
        DateTimeOffset AssignedAt,
        DateTimeOffset? SupersededAt
    );

    protected sealed record DriverResponse(
        Guid Id,
        Guid UserId,
        string DisplayName,
        string MtmDriverNumber,
        bool IsActive
    );

    protected sealed record DriverTripResponse(string TripNumber, string NextAction);

    protected sealed record DriverTripEventResponse(
        string Type,
        DateTimeOffset DeviceCapturedAt,
        DateTimeOffset ReceivedAt,
        string? OutcomeReason,
        string? Note,
        bool? TripLogSigned
    );

    protected sealed class TestKeycloakUserAdministration : IKeycloakUserAdministration
    {
        public Task<string> CreateDriverAsync(
            string email,
            string temporaryPassword,
            string organizationId,
            CancellationToken cancellationToken
        ) => Task.FromResult($"test-{email}");

        public Task ResetPasswordAsync(string subject, string temporaryPassword, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeleteUserAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    protected sealed class TestDriverWorkClock : IDriverWorkClock
    {
        private DateTimeOffset utcNow;

        public TestDriverWorkClock(DateTimeOffset utcNow) => this.utcNow = utcNow;

        public DateTimeOffset UtcNow => utcNow = utcNow.AddSeconds(1);
    }
}
