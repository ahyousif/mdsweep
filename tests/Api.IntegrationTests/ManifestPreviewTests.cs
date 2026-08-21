using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mdsweep.Api.Infrastructure;
using Mdsweep.Api.Features.ManifestImports;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Api.Features.Dispatch;
using Mdsweep.Api.Features.DriverWork;
using Testcontainers.PostgreSql;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ManifestPreviewTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();
    private WebApplicationFactory<Program> application = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(database.GetConnectionString()));
                services.RemoveAll<IKeycloakUserAdministration>();
                services.AddSingleton<IKeycloakUserAdministration, TestKeycloakUserAdministration>();
                services.RemoveAll<IDriverWorkClock>();
                services.AddSingleton<IDriverWorkClock>(new TestDriverWorkClock(new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero)));
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, DispatcherAuthenticationHandler>("Test", _ => { });
            });
        });
        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var providerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var appUser = new AppUser { KeycloakSubject = "dispatcher-test" };
        db.Providers.Add(new Provider { Id = providerId, Name = "Synthetic Provider", KeycloakOrganizationId = "synthetic-provider" });
        db.AppUsers.Add(appUser);
        db.ProviderMemberships.Add(new ProviderMembership { ProviderId = providerId, AppUserId = appUser.Id, Role = "Dispatcher" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Preview_reports_exceptions_without_persisting_trips()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        await using var file = File.OpenRead(FixturePath("mtm-manifest.csv"));
        using var form = new MultipartFormDataContent
        {
            { new StreamContent(file), "file", "mtm-manifest.csv" }
        };

        using var response = await client.PostAsync("/api/manifest-imports/preview", form);

        response.EnsureSuccessStatusCode();
        var preview = await response.Content.ReadFromJsonAsync<PreviewResponse>();
        Assert.NotNull(preview);
        Assert.Equal(2, preview.Ready);
        Assert.Equal(1, preview.Warning);
        Assert.Equal(1, preview.Blocked);
        Assert.Equal(4, preview.Rows.Count);
        Assert.Equal([new DateOnly(2026, 9, 15)], preview.ServiceDates);
        Assert.False(preview.Rows.Single(x => x.TripNumber == "SYNTH200A").IsActive);

        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await db.Trips.CountAsync());
    }

    [Fact]
    public async Task Dispatcher_realm_role_does_not_override_a_driver_membership_for_the_active_provider()
    {
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var membership = await db.ProviderMemberships.SingleAsync();
            db.ProviderMemberships.Remove(membership);
            db.ProviderMemberships.Add(new ProviderMembership
            {
                ProviderId = membership.ProviderId,
                AppUserId = membership.AppUserId,
                Role = "Driver"
            });
            await db.SaveChangesAsync();
        }

        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using var response = await client.GetAsync("/api/service-days/2026-09-15/trips");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        using var driverManagementResponse = await client.GetAsync("/api/drivers");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, driverManagementResponse.StatusCode);
    }

    [Fact]
    public async Task Dispatcher_can_assign_a_journey_then_reassign_one_trip_with_history()
    {
        var providerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid driverId;
        Guid vehicleId;
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var driverUser = new AppUser { KeycloakSubject = "driver-test" };
            db.AppUsers.Add(driverUser);
            db.ProviderMemberships.Add(new ProviderMembership { ProviderId = providerId, AppUserId = driverUser.Id, Role = "Driver" });
            var driver = new Driver { ProviderId = providerId, AppUserId = driverUser.Id, DisplayName = "Synthetic Driver", MtmDriverNumber = "DRV-1" };
            var vehicle = new Vehicle { ProviderId = providerId, DisplayName = "Van 1", Vin = "SYNTHETICVIN00001" };
            db.Drivers.Add(driver); db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();
            driverId = driver.Id; vehicleId = vehicle.Id;
        }

        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(client, Manifest(
            Row("ASSIGN100A", "VALID", "0915", "100 First St", "200 Main St"),
            Row("ASSIGN100B", "VALID", "1015", "100 First St", "200 Main St")));
        using var apply = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        apply.EnsureSuccessStatusCode();
        using var journeyResponse = await client.PostAsJsonAsync("/api/journeys/ASSIGN100/assignments", new { driverId, vehicleId });
        journeyResponse.EnsureSuccessStatusCode();
        using var tripResponse = await client.PostAsJsonAsync("/api/trips/ASSIGN100A/assignments", new { driverId, vehicleId });
        tripResponse.EnsureSuccessStatusCode();

        var history = await client.GetFromJsonAsync<List<AssignmentResponse>>("/api/trips/ASSIGN100A/assignments");
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.NotNull(history[0].SupersededAt);
        Assert.Null(history[1].SupersededAt);
    }

    [Fact]
    public async Task Dispatcher_can_create_and_reset_driver_access_without_exposing_keycloak_to_the_client()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        using var create = await client.PostAsJsonAsync("/api/drivers/access", new { email = "driver2@example.test", temporaryPassword = "Temporary-42!", displayName = "Driver Two", mtmDriverNumber = "DRV-2" });
        create.EnsureSuccessStatusCode();
        var driver = await create.Content.ReadFromJsonAsync<DriverResponse>();
        Assert.NotNull(driver);
        using var reset = await client.PostAsJsonAsync($"/api/drivers/{driver.Id}/reset-access", new { temporaryPassword = "Changed-42!" });
        Assert.Equal(System.Net.HttpStatusCode.NoContent, reset.StatusCode);
        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Drivers.AnyAsync(x => x.Id == driver.Id && x.MtmDriverNumber == "DRV-2"));
    }

    [Fact]
    public async Task Dispatcher_cannot_assign_an_inactive_broker_trip()
    {
        var (driverId, vehicleId) = await AddActiveResources();
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(client, Manifest(Row("INACTIVE1", "TURN BACK", "0915", "100 First St", "200 Main St")));
        using var apply = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        apply.EnsureSuccessStatusCode();
        using var response = await client.PostAsJsonAsync("/api/trips/INACTIVE1/assignments", new { driverId, vehicleId });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dispatcher_cannot_assign_with_an_inactive_driver()
    {
        var (driverId, vehicleId) = await AddActiveResources();
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Drivers.SingleAsync(x => x.Id == driverId)).IsActive = false;
            await db.SaveChangesAsync();
        }
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(client, Manifest(Row("ACTIVE1", "VALID", "0915", "100 First St", "200 Main St")));
        using var apply = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        apply.EnsureSuccessStatusCode();
        using var response = await client.PostAsJsonAsync("/api/trips/ACTIVE1/assignments", new { driverId, vehicleId });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dispatcher_cannot_assign_with_an_inactive_vehicle()
    {
        var (driverId, vehicleId) = await AddActiveResources();
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Vehicles.SingleAsync(x => x.Id == vehicleId)).IsActive = false;
            await db.SaveChangesAsync();
        }
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(client, Manifest(Row("ACTIVE2", "VALID", "0915", "100 First St", "200 Main St")));
        using var apply = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        apply.EnsureSuccessStatusCode();
        using var response = await client.PostAsJsonAsync("/api/trips/ACTIVE2/assignments", new { driverId, vehicleId });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reassigning_one_journey_leg_to_a_different_driver_returns_a_warning()
    {
        var (firstDriverId, vehicleId) = await AddActiveResources();
        var (secondDriverId, _) = await AddActiveResources();
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(client, Manifest(Row("WARNING100A", "VALID", "0915", "100 First St", "200 Main St"), Row("WARNING100B", "VALID", "1015", "100 First St", "200 Main St")));
        using var apply = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        apply.EnsureSuccessStatusCode();
        using var journey = await client.PostAsJsonAsync("/api/journeys/WARNING100/assignments", new { driverId = firstDriverId, vehicleId });
        journey.EnsureSuccessStatusCode();
        using var reassign = await client.PostAsJsonAsync("/api/trips/WARNING100A/assignments", new { driverId = secondDriverId, vehicleId });
        reassign.EnsureSuccessStatusCode();
        using var result = JsonDocument.Parse(await reassign.Content.ReadAsStreamAsync());
        Assert.True(result.RootElement.GetProperty("warning").GetBoolean());
    }

    [Fact]
    public async Task Applying_preview_imports_trips_groups_journeys_and_retains_blocked_rows()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await Preview(client, "mtm-manifest.csv");

        using var applyResponse = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);

        applyResponse.EnsureSuccessStatusCode();
        var applied = await applyResponse.Content.ReadFromJsonAsync<ApplyResponse>();
        Assert.NotNull(applied);
        Assert.Equal(3, applied.Imported);
        Assert.Equal(1, applied.Blocked);

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>("/api/service-days/2026-09-15/trips");
        Assert.NotNull(serviceDay);
        Assert.Equal(3, serviceDay.Count);
        Assert.Equal(2, serviceDay.Count(x => x.JourneyKey == "SYNTH100"));
        Assert.Contains(serviceDay, x => x.TripNumber == "SYNTH200A" && !x.IsActive);

        var retainedPreview = await client.GetFromJsonAsync<PreviewResponse>($"/api/manifest-imports/{preview.PreviewId}");
        Assert.NotNull(retainedPreview);
        Assert.Contains(retainedPreview.Rows, x => x.TripNumber == "SYNTH300A" && x.Disposition == "Blocked");

        using var retryResponse = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        retryResponse.EnsureSuccessStatusCode();
        var afterRetry = await client.GetFromJsonAsync<List<ServiceDayTrip>>("/api/service-days/2026-09-15/trips");
        Assert.Equal(3, afterRetry!.Count);
    }

    [Fact]
    public async Task Preview_blocks_every_row_with_a_duplicate_trip_number()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(client, Manifest(
            Row("DUPLICATE1", "VALID", "0915", "100 First St", "200 Main St"),
            Row("DUPLICATE1", "VALID", "1015", "300 Second St", "400 Oak St")));

        Assert.Equal(0, preview.Ready);
        Assert.Equal(0, preview.Warning);
        Assert.Equal(2, preview.Blocked);
        Assert.All(preview.Rows, row =>
            Assert.Contains(row.Messages, message => message.Contains("more than once", StringComparison.OrdinalIgnoreCase)));

        using var applyResponse = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        applyResponse.EnsureSuccessStatusCode();
        var applied = await applyResponse.Content.ReadFromJsonAsync<ApplyResponse>();
        Assert.Equal(0, applied!.Imported);
    }

    [Fact]
    public async Task Revised_manifest_reconciles_broker_fields_and_keeps_import_history()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var original = await PreviewCsv(client, Manifest(Row("REVISED1", "VALID", "0915", "100 First St", "200 Main St")));
        using var firstApply = await client.PostAsync($"/api/manifest-imports/{original.PreviewId}/apply", null);
        firstApply.EnsureSuccessStatusCode();

        var revised = await PreviewCsv(client, Manifest(Row("REVISED1", "TURN BACK", "1030", "300 New St", "400 Changed St")));
        using var secondApply = await client.PostAsync($"/api/manifest-imports/{revised.PreviewId}/apply", null);
        secondApply.EnsureSuccessStatusCode();

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>("/api/service-days/2026-09-15/trips");
        var trip = Assert.Single(serviceDay!);
        Assert.Equal("TURN BACK", trip.BrokerStatus);
        Assert.Equal(new TimeOnly(10, 30), trip.AppointmentTime);
        Assert.Equal("300 New St", trip.PickupAddress);
        Assert.False(trip.IsActive);

        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var history = await db.TripBrokerImports.OrderBy(x => x.ImportedAt).ToListAsync();
        Assert.Equal(["VALID", "TURN BACK"], history.Select(x => x.BrokerStatus));
        Assert.Equal(["100 First St", "300 New St"], history.Select(x => x.PickupAddress));
    }

    [Fact]
    public async Task Repeat_import_preview_identifies_unchanged_and_broker_changed_trips_without_applying_them()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var originalCsv = Manifest(
            Row("SAME1", "VALID", "0915", "100 First St", "200 Main St"),
            Row("CHANGED1", "VALID", "1015", "300 Second St", "400 Oak St"));
        var original = await PreviewCsv(client, originalCsv);
        using var apply = await client.PostAsync($"/api/manifest-imports/{original.PreviewId}/apply", null);
        apply.EnsureSuccessStatusCode();

        var revised = await PreviewCsv(client, Manifest(
            Row("SAME1", "VALID", "0915", "100 First St", "200 Main St"),
            Row("CHANGED1", "TURN BACK", "1030", "500 Revised St", "600 Changed St"),
            Row("NEW1", "VALID", "1100", "700 New St", "800 Newer St")));

        Assert.Equal("Unchanged", revised.Rows.Single(x => x.TripNumber == "SAME1").BrokerChange);
        var changed = revised.Rows.Single(x => x.TripNumber == "CHANGED1");
        Assert.Equal("BrokerChanged", changed.BrokerChange);
        Assert.Contains(changed.Messages, message =>
            message.Contains("appointment time", StringComparison.OrdinalIgnoreCase) &&
            message.Contains("pickup address", StringComparison.OrdinalIgnoreCase) &&
            message.Contains("MTM status", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("New", revised.Rows.Single(x => x.TripNumber == "NEW1").BrokerChange);

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>("/api/service-days/2026-09-15/trips");
        Assert.Equal("VALID", serviceDay!.Single(x => x.TripNumber == "CHANGED1").BrokerStatus);
        Assert.DoesNotContain(serviceDay!, x => x.TripNumber == "NEW1");
    }

    [Fact]
    public async Task Revised_manifest_preserves_provider_scheduled_pickup_and_its_history()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var original = await PreviewCsv(client,
            Manifest(Row("SCHEDULED1", "VALID", "0915", "100 First St", "200 Main St")));
        using var firstApply = await client.PostAsync($"/api/manifest-imports/{original.PreviewId}/apply", null);
        firstApply.EnsureSuccessStatusCode();

        using var firstSchedule = await client.PutAsJsonAsync(
            "/api/trips/SCHEDULED1/scheduled-pickup-time",
            new { ScheduledPickupTime = new TimeOnly(8, 0) });
        firstSchedule.EnsureSuccessStatusCode();

        var unchangedWithProviderSchedule = await PreviewCsv(client,
            Manifest(Row("SCHEDULED1", "VALID", "0915", "100 First St", "200 Main St")));
        var unchanged = Assert.Single(unchangedWithProviderSchedule.Rows);
        Assert.Equal("Unchanged", unchanged.BrokerChange);
        Assert.True(unchanged.HasProviderOverrides);
        Assert.True(unchanged.IsActive);
        Assert.Contains(unchanged.Messages, message =>
            message.Contains("scheduled pickup time will be preserved", StringComparison.OrdinalIgnoreCase));

        using var replacementSchedule = await client.PutAsJsonAsync(
            "/api/trips/SCHEDULED1/scheduled-pickup-time",
            new { ScheduledPickupTime = new TimeOnly(8, 10) });
        replacementSchedule.EnsureSuccessStatusCode();
        using var retriedSchedule = await client.PutAsJsonAsync(
            "/api/trips/SCHEDULED1/scheduled-pickup-time",
            new { ScheduledPickupTime = new TimeOnly(8, 10) });
        retriedSchedule.EnsureSuccessStatusCode();

        var revised = await PreviewCsv(client,
            Manifest(Row("SCHEDULED1", "VALID", "1030", "300 Revised St", "400 Changed St")));
        var changed = Assert.Single(revised.Rows);
        Assert.Equal("BrokerChanged", changed.BrokerChange);
        Assert.True(changed.HasProviderOverrides);
        Assert.True(changed.IsActive);
        Assert.Contains(changed.Messages, message =>
            message.Contains("scheduled pickup time will be preserved", StringComparison.OrdinalIgnoreCase));

        using var revisedApply = await client.PostAsync($"/api/manifest-imports/{revised.PreviewId}/apply", null);
        revisedApply.EnsureSuccessStatusCode();

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>("/api/service-days/2026-09-15/trips");
        var trip = Assert.Single(serviceDay!);
        Assert.Equal(new TimeOnly(10, 30), trip.AppointmentTime);
        Assert.Equal(new TimeOnly(8, 10), trip.ScheduledPickupTime);

        var history = await client.GetFromJsonAsync<List<ScheduledPickupChange>>(
            "/api/trips/SCHEDULED1/scheduled-pickup-time/history");
        Assert.Equal([new TimeOnly(8, 0), new TimeOnly(8, 10)], history!.Select(x => x.ScheduledPickupTime));
        Assert.Equal([1L, 2L], history!.Select(x => x.Sequence));
        Assert.All(history!, change => Assert.True(Guid.TryParse(change.ChangedBy, out _)));
    }

    [Fact]
    public async Task Driver_records_ordered_trip_events_with_signature_status_and_distinct_timestamps()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);

        using var dispatcherResponse = await client.GetAsync("/api/driver-work/trips");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, dispatcherResponse.StatusCode);

        await AssignTripToAuthenticatedDriver(client, "DRIVERFLOW1");
        var assigned = await client.GetFromJsonAsync<List<DriverTripResponse>>("/api/driver-work/trips");
        var trip = Assert.Single(assigned!);
        Assert.Equal("DRIVERFLOW1", trip.TripNumber);
        Assert.Equal("ArrivedAtPickup", trip.NextAction);
        Assert.DoesNotContain("brokerStatus", await (await client.GetAsync("/api/driver-work/trips")).Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var capturedAt = new DateTimeOffset(2026, 9, 15, 8, 45, 0, TimeSpan.Zero);
        using var invalid = await client.PostAsJsonAsync("/api/driver-work/trips/DRIVERFLOW1/events", new { type = "PickedUp", deviceCapturedAt = capturedAt, tripLogSigned = (bool?)null, outcomeReason = (string?)null, note = (string?)null });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("Arrived at Pickup", await invalid.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        foreach (var (type, index) in new[] { "ArrivedAtPickup", "PickedUp", "ArrivedAtDropOff" }.Select((type, index) => (type, index)))
        {
            using var response = await client.PostAsJsonAsync("/api/driver-work/trips/DRIVERFLOW1/events", new { type, deviceCapturedAt = capturedAt.AddMinutes(index), tripLogSigned = (bool?)null, outcomeReason = (string?)null, note = (string?)null });
            response.EnsureSuccessStatusCode();
            if (type == "ArrivedAtPickup")
            {
                using var retry = await client.PostAsJsonAsync("/api/driver-work/trips/DRIVERFLOW1/events", new { type, deviceCapturedAt = capturedAt.AddMinutes(index), tripLogSigned = (bool?)null, outcomeReason = (string?)null, note = (string?)null });
                retry.EnsureSuccessStatusCode();
                Assert.Equal(System.Net.HttpStatusCode.OK, retry.StatusCode);
                using var reversed = await client.PostAsJsonAsync("/api/driver-work/trips/DRIVERFLOW1/events", new { type = "PickedUp", deviceCapturedAt = capturedAt.AddMinutes(-1), tripLogSigned = (bool?)null, outcomeReason = (string?)null, note = (string?)null });
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, reversed.StatusCode);
            }
        }

        using var missingSignature = await client.PostAsJsonAsync("/api/driver-work/trips/DRIVERFLOW1/events", new { type = "DroppedOff", deviceCapturedAt = capturedAt, tripLogSigned = (bool?)null, outcomeReason = (string?)null, note = (string?)null });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, missingSignature.StatusCode);

        using var completed = await client.PostAsJsonAsync("/api/driver-work/trips/DRIVERFLOW1/events", new { type = "DroppedOff", deviceCapturedAt = capturedAt.AddMinutes(3), tripLogSigned = false, outcomeReason = (string?)null, note = (string?)null });
        completed.EnsureSuccessStatusCode();
        var history = await client.GetFromJsonAsync<List<DriverTripEventResponse>>("/api/driver-work/trips/DRIVERFLOW1/history");
        Assert.Equal(["ArrivedAtPickup", "PickedUp", "ArrivedAtDropOff", "DroppedOff"], history!.Select(x => x.Type));
        Assert.Equal([capturedAt, capturedAt.AddMinutes(1), capturedAt.AddMinutes(2), capturedAt.AddMinutes(3)], history!.Select(x => x.DeviceCapturedAt));
        Assert.All(history!, item => Assert.NotEqual(item.DeviceCapturedAt, item.ReceivedAt));
        Assert.False(history![^1].TripLogSigned);
    }

    [Fact]
    public async Task Driver_must_choose_a_standard_reason_to_record_could_not_complete()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        await AssignTripToAuthenticatedDriver(client, "OUTCOME1");

        using var invalid = await client.PostAsJsonAsync("/api/driver-work/trips/OUTCOME1/events", new { type = "CouldNotComplete", deviceCapturedAt = DateTimeOffset.UtcNow, tripLogSigned = (bool?)null, outcomeReason = "Unknown", note = (string?)null });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);

        using var result = await client.PostAsJsonAsync("/api/driver-work/trips/OUTCOME1/events", new { type = "CouldNotComplete", deviceCapturedAt = DateTimeOffset.UtcNow, tripLogSigned = (bool?)null, outcomeReason = "PassengerNoShow", note = "Waited at pickup." });
        result.EnsureSuccessStatusCode();
        var history = await client.GetFromJsonAsync<List<DriverTripEventResponse>>("/api/driver-work/trips/OUTCOME1/history");
        var outcome = Assert.Single(history!);
        Assert.Equal("CouldNotComplete", outcome.Type);
        Assert.Equal("PassengerNoShow", outcome.OutcomeReason);
        Assert.Equal("Waited at pickup.", outcome.Note);
    }

    [Fact]
    public async Task Queued_driver_action_conflict_is_retained_when_the_trip_was_reassigned()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        await AssignTripToAuthenticatedDriver(client, "SYNC1");
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.TripAssignments.SingleAsync()).SupersededAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        using var response = await client.PostAsJsonAsync("/api/driver-work/events/sync", new { tripNumber = "SYNC1", @event = new { type = "ArrivedAtPickup", deviceCapturedAt = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero), tripLogSigned = (bool?)null, outcomeReason = (string?)null, note = (string?)null } });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        await using var conflictScope = application.Services.CreateAsyncScope();
        var conflictDb = conflictScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await conflictDb.DriverTripSyncConflicts.AnyAsync(x => x.TripNumber == "SYNC1" && x.Reason.Contains("no longer assigned")));
    }

    [Fact]
    public async Task Driver_correction_retains_the_original_event_and_requires_a_reason()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        await AssignTripToAuthenticatedDriver(client, "CORRECT1");
        var capturedAt = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero);
        using var recorded = await client.PostAsJsonAsync("/api/driver-work/trips/CORRECT1/events", new { type = "ArrivedAtPickup", deviceCapturedAt = capturedAt, tripLogSigned = (bool?)null, outcomeReason = (string?)null, note = (string?)null });
        recorded.EnsureSuccessStatusCode();
        Guid eventId;
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            eventId = await db.DriverTripEvents.Select(x => x.Id).SingleAsync();
        }
        using var invalid = await client.PostAsJsonAsync($"/api/driver-work/trips/CORRECT1/events/{eventId}/corrections", new { deviceCapturedAt = capturedAt.AddMinutes(-5), reason = "" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
        using var correction = await client.PostAsJsonAsync($"/api/driver-work/trips/CORRECT1/events/{eventId}/corrections", new { deviceCapturedAt = capturedAt.AddMinutes(-5), reason = "Driver entered the wrong minute." });
        correction.EnsureSuccessStatusCode();
        await using var correctionScope = application.Services.CreateAsyncScope();
        var correctionDb = correctionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await correctionDb.DriverTripEvents.CountAsync());
        Assert.True(await correctionDb.DriverTripEventCorrections.AnyAsync(x => x.DriverTripEventId == eventId && x.Reason == "Driver entered the wrong minute."));
    }

    public async Task DisposeAsync()
    {
        await application.DisposeAsync();
        await database.DisposeAsync();
    }

    private static string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    private static async Task<PreviewResponse> Preview(HttpClient client, string fixture)
    {
        await AddAntiforgeryToken(client);
        await using var file = File.OpenRead(FixturePath(fixture));
        using var form = new MultipartFormDataContent
        {
            { new StreamContent(file), "file", fixture }
        };
        using var response = await client.PostAsync("/api/manifest-imports/preview", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PreviewResponse>())!;
    }

    private static async Task<PreviewResponse> PreviewCsv(HttpClient client, string csv, string fileName = "manifest.csv")
    {
        await AddAntiforgeryToken(client);
        using var form = new MultipartFormDataContent
        {
            { new StringContent(csv), "file", fileName }
        };
        using var response = await client.PostAsync("/api/manifest-imports/preview", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PreviewResponse>())!;
    }

    private static async Task AddAntiforgeryToken(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<AntiforgeryResponse>("/api/auth/antiforgery");
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", result!.Token);
    }

    private async Task<(Guid DriverId, Guid VehicleId)> AddActiveResources()
    {
        var providerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var appUser = new AppUser { KeycloakSubject = $"driver-{Guid.NewGuid()}" };
        var driver = new Driver { ProviderId = providerId, AppUserId = appUser.Id, DisplayName = "Synthetic Driver", MtmDriverNumber = $"DRV-{Guid.NewGuid():N}" };
        var vehicle = new Vehicle { ProviderId = providerId, DisplayName = "Van", Vin = $"VIN{Guid.NewGuid():N}"[..17] };
        db.AppUsers.Add(appUser); db.ProviderMemberships.Add(new ProviderMembership { ProviderId = providerId, AppUserId = appUser.Id, Role = "Driver" }); db.Drivers.Add(driver); db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return (driver.Id, vehicle.Id);
    }

    private async Task AssignTripToAuthenticatedDriver(HttpClient client, string tripNumber)
    {
        var providerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid driverId;
        Guid vehicleId;
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.AppUsers.SingleAsync(x => x.KeycloakSubject == "dispatcher-test");
            var driver = new Driver { ProviderId = providerId, AppUserId = user.Id, DisplayName = "Authenticated Driver", MtmDriverNumber = $"DRV-{tripNumber}" };
            var vehicle = new Vehicle { ProviderId = providerId, DisplayName = "Driver Van", Vin = $"VIN{tripNumber}".PadRight(17, '0') };
            db.Drivers.Add(driver); db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();
            driverId = driver.Id; vehicleId = vehicle.Id;
        }

        var preview = await PreviewCsv(client, Manifest(
            Row(tripNumber, "VALID", "0915", "100 First St", "200 Main St"),
            Row($"{tripNumber}OTHER", "VALID", "1015", "300 Second St", "400 Oak St")));
        using var apply = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        apply.EnsureSuccessStatusCode();
        using var assignment = await client.PostAsJsonAsync($"/api/trips/{tripNumber}/assignments", new { driverId, vehicleId });
        assignment.EnsureSuccessStatusCode();

        await using var driverScope = application.Services.CreateAsyncScope();
        var driverDb = driverScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await driverDb.ProviderMemberships.SingleAsync(x => x.ProviderId == providerId);
        driverDb.ProviderMemberships.Remove(membership);
        driverDb.ProviderMemberships.Add(new ProviderMembership { ProviderId = providerId, AppUserId = membership.AppUserId, Role = "Driver" });
        await driverDb.SaveChangesAsync();
    }

    private static string Manifest(params string[] rows) =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Passenger Type,Vehicle Type,Will Call Flag\n" +
        string.Join('\n', rows);

    private static string Row(string tripNumber, string status, string time, string pickup, string delivery) =>
        $"09/15/2026,{delivery},{pickup},{time},{tripNumber},{status},Test,Rider,Phoenix,Mesa,Ambulatory,Cab,N";

    private sealed record PreviewResponse(Guid PreviewId, int Ready, int Warning, int Blocked, List<DateOnly> ServiceDates, List<PreviewRow> Rows);
    private sealed record PreviewRow(
        string TripNumber,
        string Disposition,
        string BrokerChange,
        bool HasProviderOverrides,
        bool IsActive,
        IReadOnlyList<string> Messages);
    private sealed record ApplyResponse(int Imported, int Blocked);
    private sealed record AntiforgeryResponse(string Token);
    private sealed record ServiceDayTrip(
        string TripNumber,
        string JourneyKey,
        string BrokerStatus,
        TimeOnly AppointmentTime,
        string PickupAddress,
        TimeOnly? ScheduledPickupTime,
        bool IsActive);
    private sealed record ScheduledPickupChange(long Sequence, TimeOnly ScheduledPickupTime, string ChangedBy);
    private sealed record AssignmentResponse(Guid DriverId, Guid VehicleId, Guid AssignedByAppUserId, DateTimeOffset AssignedAt, DateTimeOffset? SupersededAt);
    private sealed record DriverResponse(Guid Id, Guid AppUserId, string DisplayName, string MtmDriverNumber, bool IsActive);
    private sealed record DriverTripResponse(string TripNumber, string NextAction);
    private sealed record DriverTripEventResponse(string Type, DateTimeOffset DeviceCapturedAt, DateTimeOffset ReceivedAt, string? OutcomeReason, string? Note, bool? TripLogSigned);

    private sealed class TestKeycloakUserAdministration : IKeycloakUserAdministration
    {
        public Task<string> CreateDriverAsync(string email, string temporaryPassword, string organizationId, CancellationToken cancellationToken) => Task.FromResult($"test-{email}");
        public Task ResetPasswordAsync(string subject, string temporaryPassword, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteUserAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestDriverWorkClock : IDriverWorkClock
    {
        private DateTimeOffset utcNow;

        public TestDriverWorkClock(DateTimeOffset utcNow) => this.utcNow = utcNow;

        public DateTimeOffset UtcNow => utcNow = utcNow.AddSeconds(1);
    }
}
