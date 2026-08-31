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

    private async Task<TripAggregate> AddTrip(string tenantId, string brokerTripNumber)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var passenger = PassengerAggregate.Create($"MED-{brokerTripNumber}", "Synthetic", "Passenger");
        passenger.TenantId = tenantId;
        var trip = TripAggregate.Create(passenger.Id, brokerTripNumber, new BrokerTripFacts(
            new DateOnly(2026, 9, 15), new LocalTime(10, 0), "100 Sample St", "Phoenix", "200 Synthetic Way", "Mesa", "VALID", false
        ));
        trip.TenantId = tenantId;
        db.AddRange(passenger, trip);
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

    private sealed record TripResponse(Guid Id, string BrokerTripNumber, string? ScheduledPickupTime);
}
