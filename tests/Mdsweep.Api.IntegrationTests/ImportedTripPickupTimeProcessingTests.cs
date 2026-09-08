using Ardalis.Result;
using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Domain.Trips;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ImportedTripPickupTimeProcessingTests : MdsweepIntegrationTest
{
    private readonly FakeRouteEstimator routeEstimator = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IRouteDurationProvider>();
        services.AddSingleton<IRouteDurationProvider>(routeEstimator);
    }

    [Fact]
    public async Task Import_populates_pickup_time_in_the_importing_tenant()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await Upload(client, Row());
        response.EnsureSuccessStatusCode();

        var trip = await WaitForPickupTime();
        Assert.Equal(new LocalTime(9, 8), trip.ScheduledPickupTime);
        Assert.Equal(new LocalTime(9, 8), trip.CalculatedPickupTime);
        Assert.Equal(1, routeEstimator.CallCount);
        Assert.Equal("mdsw-eep2-3456", trip.TenantId);
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
        Assert.Equal(1, routeEstimator.CallCount);
    }

    private async Task<TripAggregate> WaitForPickupTime()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await using var scope = Application.Services.CreateAsyncScope();
            var trip = await scope
                .ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Trips.IgnoreQueryFilters()
                .SingleOrDefaultAsync();
            if (trip?.ScheduledPickupTime is not null)
                return trip;
            await Task.Delay(100);
        }

        throw new Xunit.Sdk.XunitException("The imported trip pickup time was not populated.");
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

    private static string Row(string willCall = "N", string tripType = "T") =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag,Trip Type\n"
        + $"09/15/2026,200 Synthetic Way,100 Sample St,10:00,TRIP-PICKUP,MED-PICKUP,VALID,Synthetic,Passenger,Phoenix,Mesa,{willCall},{tripType}";

    private sealed class FakeRouteEstimator : IRouteDurationProvider
    {
        private int callCount;
        public int CallCount => Volatile.Read(ref callCount);

        public Task<Result<Duration>> GetDurationAsync(string origin, string destination, CancellationToken ct)
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(Result.Success(Duration.FromMinutes(37)));
        }
    }
}
