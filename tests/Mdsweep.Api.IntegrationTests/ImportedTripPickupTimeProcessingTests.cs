using Ardalis.Result;
using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Trips;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ImportedTripPickupTimeProcessingTests : MdsweepIntegrationTest
{
    private readonly FakeRouteEstimator routeEstimator = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IRouteEstimateProvider>();
        services.AddSingleton<IRouteEstimateProvider>(routeEstimator);
    }

    [Fact]
    public async Task Import_uses_the_default_tenant_pickup_buffer()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await Upload(client, Row());
        response.EnsureSuccessStatusCode();

        var trip = await WaitForPickupTime();
        Assert.Equal(new LocalTime(9, 7), trip.ScheduledPickupTime);
        Assert.Equal(new LocalTime(9, 7), trip.CalculatedPickupTime);
        Assert.Equal(38, trip.EstimatedTravelMinutes);
        Assert.Equal(12_345, trip.EstimatedDistanceMeters);
        Assert.Equal(1, routeEstimator.CallCount);
        Assert.Equal("mdsw-eep2-3456", trip.TenantId);
    }

    [Fact]
    public async Task Import_uses_a_custom_tenant_pickup_buffer()
    {
        await SetPickupBufferMinutes(30);
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await Upload(client, Row());
        response.EnsureSuccessStatusCode();

        var trip = await WaitForCalculatedPickupTime(new LocalTime(8, 52));
        Assert.Equal(new LocalTime(8, 52), trip.ScheduledPickupTime);
    }

    [Fact]
    public async Task Changing_the_tenant_pickup_buffer_recalculates_the_schedule()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var initial = await Upload(client, Row());
        initial.EnsureSuccessStatusCode();
        await WaitForPickupTime();

        await SetPickupBufferMinutes(30);
        using var repeat = await Upload(client, Row());
        repeat.EnsureSuccessStatusCode();

        var trip = await WaitForCalculatedPickupTime(new LocalTime(8, 52));
        Assert.Equal(new LocalTime(8, 52), trip.ScheduledPickupTime);
        Assert.Equal(2, routeEstimator.CallCount);
    }

    [Fact]
    public async Task Unchanged_import_does_not_reestimate_the_route()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Row());
        first.EnsureSuccessStatusCode();
        await WaitForPickupTime();

        using var repeat = await Upload(client, Row());
        repeat.EnsureSuccessStatusCode();
        await Task.Delay(250);

        Assert.Equal(1, routeEstimator.CallCount);
    }

    [Fact]
    public async Task Failed_route_estimate_clears_stale_estimate_values()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var initial = await Upload(client, Row());
        initial.EnsureSuccessStatusCode();
        await WaitForPickupTime();

        routeEstimator.ShouldFail = true;
        using var changedRoute = await Upload(client, Row(deliveryAddress: "201 Different Way"));
        changedRoute.EnsureSuccessStatusCode();

        var trip = await WaitForPickupTimeCleared();
        Assert.Null(trip.EstimatedTravelMinutes);
        Assert.Null(trip.EstimatedDistanceMeters);
        Assert.Equal(2, routeEstimator.CallCount);
    }

    [Fact]
    public async Task Will_call_change_clears_the_populated_pickup_time_without_routing()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var initial = await Upload(client, Row());
        initial.EnsureSuccessStatusCode();
        await WaitForPickupTime();

        using var willCall = await Upload(client, Row(willCall: "Y", tripType: "F"));
        willCall.EnsureSuccessStatusCode();

        var trip = await WaitForPickupTimeCleared();
        Assert.Null(trip.ScheduledPickupTime);
        Assert.Null(trip.CalculatedPickupTime);
        Assert.Null(trip.EstimatedTravelMinutes);
        Assert.Null(trip.EstimatedDistanceMeters);
        Assert.Equal(1, routeEstimator.CallCount);
    }

    private Task<TripAggregate> WaitForPickupTime() => WaitForCalculatedPickupTime(new LocalTime(9, 7));

    private async Task<TripAggregate> WaitForCalculatedPickupTime(LocalTime expectedPickupTime)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await using var scope = Application.Services.CreateAsyncScope();
            var trip = await scope
                .ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Trips.IgnoreQueryFilters()
                .SingleOrDefaultAsync();
            if (trip?.CalculatedPickupTime == expectedPickupTime)
                return trip;
            await Task.Delay(100);
        }

        throw new Xunit.Sdk.XunitException("The imported trip pickup time was not populated.");
    }

    private async Task SetPickupBufferMinutes(int pickupBufferMinutes)
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = await db.Tenants.SingleAsync();
        tenant.SetPickupBufferMinutes(pickupBufferMinutes);
        await db.SaveChangesAsync();
    }

    private async Task<TripAggregate> WaitForPickupTimeCleared()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await using var scope = Application.Services.CreateAsyncScope();
            var trip = await scope
                .ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Trips.IgnoreQueryFilters()
                .SingleAsync();
            if (trip.ScheduledPickupTime is null)
                return trip;
            await Task.Delay(100);
        }

        throw new Xunit.Sdk.XunitException("The will-call import did not clear the pickup time.");
    }

    private static Task<HttpResponseMessage> Upload(HttpClient client, string csv) =>
        client.PostAsync(
            "/api/trips/import",
            new MultipartFormDataContent
            {
                { new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(csv)), "file", "trips.csv" },
            }
        );

    private static string Row(
        string willCall = "N",
        string tripType = "T",
        string deliveryAddress = "200 Synthetic Way"
    ) =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag,Trip Type\n"
        + $"09/15/2026,{deliveryAddress},100 Sample St,10:00,TRIP-PICKUP,MED-PICKUP,VALID,Synthetic,Passenger,Phoenix,Mesa,{willCall},{tripType}";

    private sealed class FakeRouteEstimator : IRouteEstimateProvider
    {
        private int callCount;
        private volatile bool shouldFail;
        public int CallCount => Volatile.Read(ref callCount);
        public bool ShouldFail
        {
            get => shouldFail;
            set => shouldFail = value;
        }

        public Task<Result<RouteEstimate>> GetEstimateAsync(string origin, string destination, CancellationToken ct)
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(
                shouldFail
                    ? Result<RouteEstimate>.Error("Synthetic route-estimation failure.")
                    : Result.Success(new RouteEstimate(Duration.FromSeconds(2_221), 12_345))
            );
        }
    }
}
