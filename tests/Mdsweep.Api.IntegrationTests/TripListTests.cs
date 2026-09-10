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
    public async Task Dispatcher_lists_only_active_tenant_trips_and_filters_by_date_range()
    {
        await AddTrip("mdsw-eep2-3456", "TRIP-14", new LocalDate(2026, 9, 14), "VALID", false);
        await AddTrip("mdsw-eep2-3456", "TRIP-15", new LocalDate(2026, 9, 15), "VALID", false);
        await AddTrip("mdsw-eep2-3456", "TRIP-16", new LocalDate(2026, 9, 16), "VALID", true);
        await AddTrip("mdsw-other-000", "TRIP-OTHER", new LocalDate(2026, 9, 15), "VALID", false);
        using var client = Application.CreateClient();

        var all = await GetTrips(client, "/api/trips");
        Assert.Equal(3, all.TotalCount);
        Assert.DoesNotContain(all.Items, trip => trip.BrokerTripNumber == "TRIP-OTHER");

        var range = await GetTrips(client, "/api/trips?startDate=2026-09-15&endDate=2026-09-16");
        Assert.Equal(["TRIP-15", "TRIP-16"], range.Items.Select(trip => trip.BrokerTripNumber));

        var trip = range.Items.Single(trip => trip.BrokerTripNumber == "TRIP-15");
        Assert.Equal("2026-09-15", trip.ServiceDate);
        Assert.Equal("100 Sample St", trip.Pickup.Address);
        Assert.Equal("Mesa", trip.Dropoff.City);
    }

    [Theory]
    [InlineData("case-123")]
    [InlineData("ADA")]
    [InlineData("lovelace")]
    [InlineData("member-42")]
    [InlineData("upper road")]
    [InlineData("phoenix")]
    [InlineData("synthetic way")]
    [InlineData("mesa")]
    public async Task Dispatcher_searches_trip_fields_case_insensitively(string search)
    {
        await AddTrip(
            "mdsw-eep2-3456",
            "CASE-123",
            new LocalDate(2026, 9, 15),
            "VALID",
            false,
            firstName: "Ada",
            lastName: "Lovelace",
            brokerMemberId: "MEMBER-42",
            pickupAddress: "100 Upper Road",
            pickupCity: "Phoenix",
            dropoffAddress: "200 Synthetic Way",
            dropoffCity: "Mesa"
        );
        await AddTrip(
            "mdsw-eep2-3456",
            "OTHER-456",
            new LocalDate(2026, 9, 15),
            "VALID",
            false,
            firstName: "Other",
            lastName: "Rider",
            pickupAddress: "300 Different Street",
            pickupCity: "Tempe",
            dropoffAddress: "400 Separate Avenue",
            dropoffCity: "Chandler"
        );
        using var client = Application.CreateClient();

        var result = await GetTrips(client, $"/api/trips?search={Uri.EscapeDataString(search)}");

        Assert.Equal(["CASE-123"], result.Items.Select(trip => trip.BrokerTripNumber));
    }

    [Fact]
    public async Task Dispatcher_filters_by_broker_status_and_will_call()
    {
        var date = new LocalDate(2026, 9, 15);
        await AddTrip("mdsw-eep2-3456", "VALID-WILL-CALL", date, "VALID", true);
        await AddTrip("mdsw-eep2-3456", "VALID-NOT-WILL-CALL", date, "VALID", false);
        await AddTrip("mdsw-eep2-3456", "TURN-BACK", date, "TURN BACK", true);
        using var client = Application.CreateClient();

        var result = await GetTrips(client, "/api/trips?brokerStatus=VALID&isWillCall=true");

        Assert.Equal(["VALID-WILL-CALL"], result.Items.Select(trip => trip.BrokerTripNumber));
    }

    [Fact]
    public async Task Dispatcher_exposes_trip_times_by_direction_and_will_call_status()
    {
        var date = new LocalDate(2026, 9, 15);
        const string tenantId = "mdsw-eep2-3456";

        await AddTrip(tenantId, "TO-TRIP", date, "VALID", false);

        await AddTrip(tenantId, "FROM-TRIP", date, "VALID", false, direction: TripDirection.From);

        await AddTrip(tenantId, "WILL-CALL-TRIP", date, "VALID", true, direction: TripDirection.From);

        using var client = Application.CreateClient();

        var trips = await GetTrips(client, "/api/trips?startDate=2026-09-15&endDate=2026-09-15");

        var toTrip = trips.Items.Single(trip => trip.BrokerTripNumber == "TO-TRIP");

        Assert.Equal("10:00:00", toTrip.AppointmentTime);
        Assert.Null(toTrip.ReturnPickupTime);

        var fromTrip = trips.Items.Single(trip => trip.BrokerTripNumber == "FROM-TRIP");

        Assert.Null(fromTrip.AppointmentTime);
        Assert.Equal("10:00:00", fromTrip.ReturnPickupTime);

        var willCallTrip = trips.Items.Single(trip => trip.BrokerTripNumber == "WILL-CALL-TRIP");

        Assert.Null(willCallTrip.AppointmentTime);
        Assert.Null(willCallTrip.ReturnPickupTime);
    }

    [Fact]
    public async Task Dispatcher_paginates_within_its_safety_limits()
    {
        var date = new LocalDate(2026, 9, 15);
        await AddTrip("mdsw-eep2-3456", "TRIP-A", date, "VALID", false);
        await AddTrip("mdsw-eep2-3456", "TRIP-B", date, "VALID", false);
        await AddTrip("mdsw-eep2-3456", "TRIP-C", date, "VALID", false);
        using var client = Application.CreateClient();

        var firstPage = await GetTrips(client, "/api/trips?page=1&pageSize=2");
        var secondPage = await GetTrips(client, "/api/trips?page=2&pageSize=2");
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Single(secondPage.Items);
        Assert.Empty(firstPage.Items.Select(trip => trip.Id).Intersect(secondPage.Items.Select(trip => trip.Id)));

        using var invalidPage = await client.GetAsync("/api/trips?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
        await AssertValidationError(invalidPage, "page");

        using var oversizedPage = await client.GetAsync("/api/trips?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, oversizedPage.StatusCode);
        await AssertValidationError(oversizedPage, "pageSize");
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
        string brokerStatus,
        bool isWillCall,
        TripDirection direction = TripDirection.To,
        string firstName = "Synthetic",
        string lastName = "Passenger",
        string? brokerMemberId = null,
        string pickupAddress = "100 Sample St",
        string pickupCity = "Phoenix",
        string dropoffAddress = "200 Synthetic Way",
        string dropoffCity = "Mesa"
    )
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new ApplicationDbContext(options);
        var passenger = PassengerAggregate.Create(brokerMemberId ?? $"MED-{brokerTripNumber}", firstName, lastName);
        passenger.TenantId = tenantId;
        var trip = TripAggregate.Create(
            passenger.Id,
            brokerTripNumber,
            new BrokerTripData(
                serviceDate,
                direction == TripDirection.To ? new LocalTime(10, 0) : null,
                direction == TripDirection.From && !isWillCall ? new LocalTime(10, 0) : null,
                direction,
                isWillCall,
                pickupAddress,
                pickupCity,
                null,
                null,
                dropoffAddress,
                dropoffCity,
                null,
                null,
                brokerStatus,
                null,
                null,
                null,
                null
            )
        );
        trip.TenantId = tenantId;
        db.AddRange(passenger, trip);
        await db.SaveChangesAsync();
    }

    private sealed record PagedTripResponse(
        List<TripResponse> Items,
        long TotalCount,
        int Page,
        int PageSize,
        long TotalPages
    );

    private sealed record TripResponse(
        Guid Id,
        string BrokerTripNumber,
        string ServiceDate,
        string? AppointmentTime,
        string? ReturnPickupTime,
        AddressResponse Pickup,
        AddressResponse Dropoff
    );

    private sealed record AddressResponse(string Address, string City, string? State, string? Zip);
}
