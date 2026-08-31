using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.DriverWork;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.Identity;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public sealed class DispatchWorkflowTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Dispatcher_realm_role_does_not_override_a_driver_membership_for_the_active_provider()
    {
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var membership = await db.ProviderMemberships.SingleAsync();
            db.ProviderMemberships.Remove(membership);
            db.ProviderMemberships.Add(
                new ProviderMembership
                {
                    ProviderId = membership.ProviderId,
                    AppUserId = membership.AppUserId,
                    Role = "Driver",
                }
            );
            await db.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
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
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var driverUser = new AppUser { KeycloakSubject = "driver-test" };
            db.AppUsers.Add(driverUser);
            db.ProviderMemberships.Add(
                new ProviderMembership
                {
                    ProviderId = providerId,
                    AppUserId = driverUser.Id,
                    Role = "Driver",
                }
            );
            var driver = new Driver
            {
                ProviderId = providerId,
                AppUserId = driverUser.Id,
                DisplayName = "Synthetic Driver",
                MtmDriverNumber = "DRV-1",
            };
            var vehicle = new Vehicle
            {
                ProviderId = providerId,
                DisplayName = "Van 1",
                Vin = "SYNTHETICVIN00001",
            };
            db.Drivers.Add(driver);
            db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();
            driverId = driver.Id;
            vehicleId = vehicle.Id;
        }

        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(
            client,
            Manifest(
                Row("ASSIGN100A", "VALID", "0915", "100 First St", "200 Main St"),
                Row("ASSIGN100B", "VALID", "1015", "100 First St", "200 Main St")
            )
        );
        using var apply = await client.PostAsync(
            $"/api/manifest-imports/{preview.PreviewId}/apply",
            null
        );
        apply.EnsureSuccessStatusCode();
        using var journeyResponse = await client.PostAsJsonAsync(
            "/api/journeys/ASSIGN100/assignments",
            new { driverId, vehicleId }
        );
        journeyResponse.EnsureSuccessStatusCode();
        using var tripResponse = await client.PostAsJsonAsync(
            "/api/trips/ASSIGN100A/assignments",
            new { driverId, vehicleId }
        );
        tripResponse.EnsureSuccessStatusCode();

        var history = await client.GetFromJsonAsync<List<AssignmentResponse>>(
            "/api/trips/ASSIGN100A/assignments"
        );
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.NotNull(history[0].SupersededAt);
        Assert.Null(history[1].SupersededAt);
    }

    [Fact]
    public async Task Dispatcher_can_create_and_reset_driver_access_without_exposing_keycloak_to_the_client()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        using var create = await client.PostAsJsonAsync(
            "/api/drivers/access",
            new
            {
                email = "driver2@example.test",
                temporaryPassword = "P@ssw0rd!",
                displayName = "Driver Two",
                mtmDriverNumber = "DRV-2",
            }
        );
        create.EnsureSuccessStatusCode();
        var driver = await create.Content.ReadFromJsonAsync<DriverResponse>();
        Assert.NotNull(driver);
        using var reset = await client.PostAsJsonAsync(
            $"/api/drivers/{driver.Id}/reset-access",
            new { temporaryPassword = "P@ssw0rd!" }
        );
        Assert.Equal(System.Net.HttpStatusCode.NoContent, reset.StatusCode);
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(
            await db.Drivers.AnyAsync(x => x.Id == driver.Id && x.MtmDriverNumber == "DRV-2")
        );
    }

    [Fact]
    public async Task Dispatcher_cannot_assign_an_inactive_broker_trip()
    {
        var (driverId, vehicleId) = await AddActiveResources();
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(
            client,
            Manifest(Row("INACTIVE1", "TURN BACK", "0915", "100 First St", "200 Main St"))
        );
        using var apply = await client.PostAsync(
            $"/api/manifest-imports/{preview.PreviewId}/apply",
            null
        );
        apply.EnsureSuccessStatusCode();
        using var response = await client.PostAsJsonAsync(
            "/api/trips/INACTIVE1/assignments",
            new { driverId, vehicleId }
        );
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dispatcher_cannot_assign_with_an_inactive_driver()
    {
        var (driverId, vehicleId) = await AddActiveResources();
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Drivers.SingleAsync(x => x.Id == driverId)).IsActive = false;
            await db.SaveChangesAsync();
        }
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(
            client,
            Manifest(Row("ACTIVE1", "VALID", "0915", "100 First St", "200 Main St"))
        );
        using var apply = await client.PostAsync(
            $"/api/manifest-imports/{preview.PreviewId}/apply",
            null
        );
        apply.EnsureSuccessStatusCode();
        using var response = await client.PostAsJsonAsync(
            "/api/trips/ACTIVE1/assignments",
            new { driverId, vehicleId }
        );
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dispatcher_cannot_assign_with_an_inactive_vehicle()
    {
        var (driverId, vehicleId) = await AddActiveResources();
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Vehicles.SingleAsync(x => x.Id == vehicleId)).IsActive = false;
            await db.SaveChangesAsync();
        }
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(
            client,
            Manifest(Row("ACTIVE2", "VALID", "0915", "100 First St", "200 Main St"))
        );
        using var apply = await client.PostAsync(
            $"/api/manifest-imports/{preview.PreviewId}/apply",
            null
        );
        apply.EnsureSuccessStatusCode();
        using var response = await client.PostAsJsonAsync(
            "/api/trips/ACTIVE2/assignments",
            new { driverId, vehicleId }
        );
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reassigning_one_journey_leg_to_a_different_driver_returns_a_warning()
    {
        var (firstDriverId, vehicleId) = await AddActiveResources();
        var (secondDriverId, _) = await AddActiveResources();
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(
            client,
            Manifest(
                Row("WARNING100A", "VALID", "0915", "100 First St", "200 Main St"),
                Row("WARNING100B", "VALID", "1015", "100 First St", "200 Main St")
            )
        );
        using var apply = await client.PostAsync(
            $"/api/manifest-imports/{preview.PreviewId}/apply",
            null
        );
        apply.EnsureSuccessStatusCode();
        using var journey = await client.PostAsJsonAsync(
            "/api/journeys/WARNING100/assignments",
            new { driverId = firstDriverId, vehicleId }
        );
        journey.EnsureSuccessStatusCode();
        using var reassign = await client.PostAsJsonAsync(
            "/api/trips/WARNING100A/assignments",
            new { driverId = secondDriverId, vehicleId }
        );
        reassign.EnsureSuccessStatusCode();
        using var result = JsonDocument.Parse(await reassign.Content.ReadAsStreamAsync());
        Assert.True(result.RootElement.GetProperty("warning").GetBoolean());
    }
}
