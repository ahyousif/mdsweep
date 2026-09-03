using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class TripListTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Dispatcher_can_list_trips_with_default_parameters_then_filter_sort_and_page_only_the_active_tenants_trips()
    {
        await AddTrip("mdsw-eep2-3456", "TRIP-A", new LocalDate(2026, 9, 15), new LocalTime(10, 0), "VALID", false);
        await AddTrip("mdsw-eep2-3456", "TRIP-B", new LocalDate(2026, 9, 15), new LocalTime(9, 0), "VALID", true);
        await AddTrip("mdsw-eep2-3456", "TRIP-C", new LocalDate(2026, 9, 16), new LocalTime(11, 0), "TURN BACK", false);
        await AddTrip("mdsw-eep2-3456", "TRIP-D", new LocalDate(2026, 9, 15), new LocalTime(11, 0), "VALID", false);
        await AddTrip("mdsw-other-000", "TRIP-OTHER", new LocalDate(2026, 9, 15), new LocalTime(8, 0), "VALID", false);
        using var client = Application.CreateClient();

        var unfiltered = await GetTrips(client, "/api/trips");
        Assert.Equal(4, unfiltered.TotalCount);
        Assert.Equal(["TRIP-B", "TRIP-A", "TRIP-D", "TRIP-C"], unfiltered.Items.Select(trip => trip.BrokerTripNumber));
        Assert.DoesNotContain(unfiltered.Items, trip => trip.BrokerTripNumber == "TRIP-OTHER");

        var serviceDate = await GetTrips(client, "/api/trips?serviceDate=2026-09-15");
        Assert.Equal(3, serviceDate.TotalCount);
        Assert.All(serviceDate.Items, trip => Assert.Equal("2026-09-15", trip.ServiceDate));
        var tripA = serviceDate.Items.Single(trip => trip.BrokerTripNumber == "TRIP-A");
        Assert.Equal("100 Sample St", tripA.PickupAddress);
        Assert.Equal("Mesa", tripA.DropoffCity);

        var combinedFilters = await GetTrips(
            client,
            "/api/trips?serviceDate=2026-09-15&brokerStatus=VALID&isWillCall=false&sortBy=brokerTripNumber&sortDirection=descending"
        );
        Assert.Equal(2, combinedFilters.TotalCount);
        Assert.Equal(["TRIP-D", "TRIP-A"], combinedFilters.Items.Select(trip => trip.BrokerTripNumber));

        var ascending = await GetTrips(client, "/api/trips?sortBy=appointmentTime&sortDirection=ascending");
        var descending = await GetTrips(client, "/api/trips?sortBy=appointmentTime&sortDirection=descending");
        Assert.Equal(["TRIP-B", "TRIP-A", "TRIP-D", "TRIP-C"], ascending.Items.Select(trip => trip.BrokerTripNumber));
        Assert.Equal(["TRIP-C", "TRIP-D", "TRIP-A", "TRIP-B"], descending.Items.Select(trip => trip.BrokerTripNumber));

        var firstPage = await GetTrips(client, "/api/trips?page=1&pageSize=2");
        var secondPage = await GetTrips(client, "/api/trips?page=2&pageSize=2");
        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(["TRIP-B", "TRIP-A"], firstPage.Items.Select(trip => trip.BrokerTripNumber));
        Assert.Equal(["TRIP-D", "TRIP-C"], secondPage.Items.Select(trip => trip.BrokerTripNumber));
    }

    [Theory]
    [InlineData("/api/trips?page=0", "page")]
    [InlineData("/api/trips?pageSize=101", "pageSize")]
    [InlineData("/api/trips?sortBy=999", "sortBy")]
    [InlineData("/api/trips?sortDirection=999", "sortDirection")]
    [InlineData("/api/trips?serviceDate=15-09-2026", "serviceDate")]
    public async Task Dispatcher_receives_an_actionable_validation_response_for_unsupported_list_parameters(
        string url,
        string validationKey
    )
    {
        using var client = Application.CreateClient();

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertValidationError(response, validationKey);
    }

    private static async Task<PagedTripResponse> GetTrips(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedTripResponse>())!;
    }

    private static async Task AssertValidationError(HttpResponseMessage response, string validationKey)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var problem = document.RootElement;

        if (problem.TryGetProperty("errors", out var errors))
        {
            Assert.True(errors.TryGetProperty(validationKey, out var messages));
            Assert.NotEmpty(messages.EnumerateArray());
            return;
        }

        Assert.Equal(validationKey, problem.GetProperty("parameter").GetString(), ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
    }

    private async Task AddTrip(
        string tenantId,
        string brokerTripNumber,
        LocalDate serviceDate,
        LocalTime appointmentTime,
        string brokerStatus,
        bool isWillCall
    )
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var passenger = PassengerAggregate.Create($"MED-{brokerTripNumber}", "Synthetic", "Passenger");
        passenger.TenantId = tenantId;
        var trip = TripAggregate.Create(
            passenger.Id,
            brokerTripNumber,
            new BrokerTripData(
                serviceDate.ToDateOnly(),
                appointmentTime,
                "100 Sample St",
                "Phoenix",
                "200 Synthetic Way",
                "Mesa",
                brokerStatus,
                isWillCall
            )
        );
        trip.TenantId = tenantId;
        db.AddRange(passenger, trip);
        await db.SaveChangesAsync();
    }

    private sealed record PagedTripResponse(List<TripResponse> Items, int TotalCount, int Page, int PageSize);

    private sealed record TripResponse(
        Guid Id,
        string BrokerTripNumber,
        string ServiceDate,
        string PickupAddress,
        string PickupCity,
        string DropoffAddress,
        string DropoffCity
    );
}
