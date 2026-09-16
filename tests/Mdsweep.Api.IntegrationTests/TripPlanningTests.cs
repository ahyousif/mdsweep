using System.Net;
using System.Net.Http.Json;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class TripPlanningTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Dispatcher_can_set_and_retrieve_a_tenant_trip_scheduled_pickup_time()
    {
        var trip = await AddTrip("mdsw-eep2-3456", "TRIP-PLANNED");
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var setResponse = await client.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/scheduled-pickup-time",
            new { scheduledPickupTime = "09:15:00" }
        );
        setResponse.EnsureSuccessStatusCode();

        using var getResponse = await client.GetAsync($"/api/trips/{trip.Id}");
        getResponse.EnsureSuccessStatusCode();
        var result = await getResponse.Content.ReadFromJsonAsync<TripResponse>();
        Assert.NotNull(result);
        Assert.Equal(trip.Id, result.Id);
        Assert.Equal("TRIP-PLANNED", result.BrokerTripNumber);
        Assert.Equal("09:15:00", result.ScheduledPickupTime);

        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedTrip = await db.Trips.IgnoreQueryFilters().SingleAsync(saved => saved.Id == trip.Id);
        Assert.Equal("mdsw-eep2-3456", storedTrip.TenantId);
        Assert.Equal(trip.BrokerData, storedTrip.BrokerData);
    }

    [Fact]
    public async Task Dispatcher_cannot_read_or_set_another_tenants_trip_scheduled_pickup_time()
    {
        var trip = await AddTrip("mdsw-other-000", "TRIP-OTHER-TENANT");
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var getResponse = await client.GetAsync($"/api/trips/{trip.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        using var setResponse = await client.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/scheduled-pickup-time",
            new { scheduledPickupTime = "09:15:00" }
        );
        Assert.Equal(HttpStatusCode.NotFound, setResponse.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Dispatcher_can_change_and_clear_override_preserving_fallback(bool calculated)
    {
        var trip = await AddTrip("mdsw-eep2-3456", "TRIP-FALLBACK", calculated);
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        foreach (var time in new[] { "09:15:00", "08:30:00" })
        {
            using var response = await client.PutAsJsonAsync(
                $"/api/trips/{trip.Id}/scheduled-pickup-time", new { scheduledPickupTime = time });
            response.EnsureSuccessStatusCode();
            var result = await client.GetFromJsonAsync<TripResponse>($"/api/trips/{trip.Id}");
            Assert.Equal(time, result!.ScheduledPickupTime);
            Assert.Equal(time, result.ManualPickupTime);
        }
        using var cleared = await client.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/scheduled-pickup-time", new { scheduledPickupTime = (string?)null });
        cleared.EnsureSuccessStatusCode();
        var fallback = await client.GetFromJsonAsync<TripResponse>($"/api/trips/{trip.Id}");
        Assert.Null(fallback!.ManualPickupTime);
        Assert.Equal(calculated ? "09:03:00" : "09:45:00", fallback.ScheduledPickupTime);
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await db.Trips.IgnoreQueryFilters().SingleAsync(saved => saved.Id == trip.Id);
        Assert.Null(stored.ManualPickupTime);
        Assert.Equal(trip.BrokerData, stored.BrokerData);
    }

    [Fact]
    public async Task Missing_trip_returns_not_found_for_set_and_clear()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        foreach (var time in new string?[] { "09:15:00", null })
        {
            using var response = await client.PutAsJsonAsync(
                $"/api/trips/{Guid.NewGuid()}/scheduled-pickup-time", new { scheduledPickupTime = time });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task Driver_cannot_change_scheduled_pickup()
    {
        var trip = await AddTrip("mdsw-eep2-3456", "TRIP-FORBIDDEN");
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await db.TenantMemberships.IgnoreQueryFilters().SingleAsync();
        membership.SetRoles(["Driver"]);
        await db.SaveChangesAsync();
        using var response = await client.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/scheduled-pickup-time", new { scheduledPickupTime = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_user_cannot_set_or_clear_scheduled_pickup()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        foreach (var time in new string?[] { "09:15:00", null })
        {
            using var response = await client.PutAsJsonAsync(
                $"/api/trips/{Guid.NewGuid()}/scheduled-pickup-time", new { scheduledPickupTime = time });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    private async Task<TripAggregate> AddTrip(string tenantId, string brokerTripNumber, bool? calculated = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new ApplicationDbContext(options);
        var passenger = PassengerAggregate.Create($"MED-{brokerTripNumber}", "Synthetic", "Passenger");
        passenger.TenantId = tenantId;
        var journey = JourneyAggregate.Create();
        journey.TenantId = tenantId;
        var trip = TripAggregate.Create(
            journey.Id,
            passenger.Id,
            brokerTripNumber,
            new BrokerTripData(
                new LocalDate(2026, 9, 15),
                new LocalTime(10, 0),
                calculated == false ? new LocalTime(9, 45) : null,
                TripDirection.To,
                false,
                "100 Sample St",
                "Phoenix",
                null,
                null,
                "200 Synthetic Way",
                "Mesa",
                null,
                null,
                "VALID",
                null,
                null,
                null,
                null
            )
        );
        if (calculated == true) trip.ApplyRouteEstimate(Duration.FromMinutes(42), 47475, 15);
        trip.TenantId = tenantId;
        db.AddRange(passenger, journey, trip);
        await db.SaveChangesAsync();
        Assert.Equal(tenantId, (await db.Trips.SingleAsync(saved => saved.Id == trip.Id)).TenantId);
        await using var scope = Application.Services.CreateAsyncScope();
        var applicationDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            tenantId,
            (await applicationDb.Trips.IgnoreQueryFilters().SingleAsync(saved => saved.Id == trip.Id)).TenantId
        );
        return trip;
    }

    private sealed record TripResponse(Guid Id, string BrokerTripNumber, string? ScheduledPickupTime, string? ManualPickupTime);
}
