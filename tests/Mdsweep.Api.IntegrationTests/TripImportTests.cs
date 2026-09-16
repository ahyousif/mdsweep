using System.Net;
using System.Net.Http.Json;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class TripImportTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Csv_import_adds_trips_and_reimport_is_unchanged()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Csv());
        var firstResult = await first.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(firstResult);
        Assert.Equal(1, firstResult.ReadyCount);
        await using var firstLookup = Application.Services.CreateAsyncScope();
        var firstDb = firstLookup.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var firstTrip = await firstDb.Trips.IgnoreQueryFilters().SingleAsync();
        var firstTripId = firstTrip.Id;
        var firstJourneyId = firstTrip.JourneyId;
        using var repeat = await Upload(client, Csv());
        var repeatResult = await repeat.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(repeatResult);
        Assert.Equal(1, repeatResult.ReadyCount);
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repeatedTrip = await db.Trips.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(firstTripId, repeatedTrip.Id);
        Assert.Equal(firstJourneyId, repeatedTrip.JourneyId);
        Assert.Single(await db.Journeys.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Xlsx_import_adds_trips()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var response = await Upload(client, "trips.xlsx", Xlsx());
        var result = await response.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.ReadyCount);
    }

    [Fact]
    public async Task Reciprocal_trips_from_the_same_import_share_one_journey()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var response = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        response.EnsureSuccessStatusCode();

        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var trips = await db.Trips.IgnoreQueryFilters().ToListAsync();
        Assert.Equal(2, trips.Count);
        Assert.Single(trips.Select(trip => trip.JourneyId).Distinct());
        Assert.Single(await db.Journeys.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Import_retains_passenger_type_and_special_needs_as_broker_facts()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await Upload(
            client,
            "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag,Passenger Type,Special Needs,Trip Type\n"
                + "09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-MOBILITY,MED-MOBILITY,VALID,Synthetic,Passenger,Phoenix,Mesa,N,Wheel Chair,Cannot Transfer,T"
        );
        response.EnsureSuccessStatusCode();

        await using var scope = Application.Services.CreateAsyncScope();
        var trip = await scope
            .ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Trips.IgnoreQueryFilters()
            .SingleAsync();
        Assert.Equal("Wheel Chair", trip.BrokerData.PassengerType);
        Assert.Equal("Cannot Transfer", trip.BrokerData.SpecialNeeds);
    }

    [Fact]
    public async Task Changed_broker_data_preserves_scheduled_pickup_time()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var initial = await Upload(client, Csv());
        initial.EnsureSuccessStatusCode();
        await using var lookup = Application.Services.CreateAsyncScope();
        var tripId = (
            await lookup
                .ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Trips.IgnoreQueryFilters()
                .SingleAsync()
        ).Id;
        using var schedule = await client.PutAsJsonAsync(
            $"/api/trips/{tripId}/scheduled-pickup-time",
            new { scheduledPickupTime = "08:30:00" }
        );
        schedule.EnsureSuccessStatusCode();
        using var changed = await Upload(
            client,
            Csv("09/16/2026,201 Way,101 St,10:15,TRIP-100,MED-100,VALID,Synthetic,Passenger,Phoenix,Mesa,N")
        );
        var result = await changed.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.ReadyCount);
        await using var verification = Application.Services.CreateAsyncScope();
        var trip = await verification
            .ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Trips.IgnoreQueryFilters()
            .SingleAsync();
        Assert.Equal(new LocalTime(8, 30), trip.ScheduledPickupTime);
    }

    [Fact]
    public async Task Later_reciprocal_trip_joins_the_existing_single_leg_journey()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001")));
        first.EnsureSuccessStatusCode();

        await using var firstScope = Application.Services.CreateAsyncScope();
        var firstTrip = await firstScope
            .ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Trips.IgnoreQueryFilters()
            .SingleAsync();
        var originalTripId = firstTrip.Id;
        var originalJourneyId = firstTrip.JourneyId;

        using var second = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        second.EnsureSuccessStatusCode();

        await using var verification = Application.Services.CreateAsyncScope();
        var trips = await verification
            .ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Trips.IgnoreQueryFilters()
            .OrderBy(trip => trip.BrokerTripNumber)
            .ToListAsync();
        Assert.Equal(2, trips.Count);
        Assert.Equal(originalTripId, trips[0].Id);
        Assert.All(trips, trip => Assert.Equal(originalJourneyId, trip.JourneyId));
        Assert.Single(
            await verification
                .ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Journeys.IgnoreQueryFilters()
                .ToListAsync()
        );
    }

    [Fact]
    public async Task Reimport_preserves_dispatcher_changed_journey_membership()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001")));
        first.EnsureSuccessStatusCode();

        Guid authoritativeJourneyId;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using (var db = new ApplicationDbContext(options))
        {
            var trip = await db.Trips.IgnoreQueryFilters().SingleAsync();
            var authoritativeJourney = JourneyAggregate.Create();
            authoritativeJourney.TenantId = trip.TenantId;
            db.Journeys.Add(authoritativeJourney);
            trip.ChangeJourney(authoritativeJourney.Id);
            await db.SaveChangesAsync();
            authoritativeJourneyId = authoritativeJourney.Id;
        }

        using var repeat = await Upload(client, Manifest(Outbound("1001")));
        repeat.EnsureSuccessStatusCode();

        await using var verification = Application.Services.CreateAsyncScope();
        var stored = await verification
            .ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Trips.IgnoreQueryFilters()
            .SingleAsync();
        Assert.Equal(authoritativeJourneyId, stored.JourneyId);
    }

    [Fact]
    public async Task Later_manifest_missing_a_trip_does_not_delete_it()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();
        using var later = await Upload(client, Manifest(Outbound("1001")));
        later.EnsureSuccessStatusCode();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new ApplicationDbContext(options);
        Assert.Equal(2, await db.Trips.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Persistence_allows_three_trips_in_one_journey()
    {
        const string tenantId = "mdsw-eep2-3456";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new ApplicationDbContext(options);
        var passenger = PassengerAggregate.Create("MED-THREE", "Synthetic", "Passenger");
        passenger.TenantId = tenantId;
        var journey = JourneyAggregate.Create();
        journey.TenantId = tenantId;
        var trips = Enumerable
            .Range(1, 3)
            .Select(index =>
                TripAggregate.Create(
                    journey.Id,
                    passenger.Id,
                    $"THREE-{index}",
                    new BrokerTripData(
                        new LocalDate(2026, 9, 15),
                        new LocalTime(9 + index, 0),
                        null,
                        TripDirection.To,
                        false,
                        $"{index}00 Synthetic St",
                        "Phoenix",
                        null,
                        null,
                        $"{index}00 Clinic Ave",
                        "Mesa",
                        null,
                        null,
                        "VALID",
                        null,
                        null,
                        null,
                        null
                    )
                )
            )
            .ToArray();
        foreach (var trip in trips)
        {
            trip.TenantId = tenantId;
        }

        db.Add(passenger);
        db.Add(journey);
        db.AddRange(trips);
        await db.SaveChangesAsync();

        Assert.Equal(3, await db.Trips.IgnoreQueryFilters().CountAsync(trip => trip.JourneyId == journey.Id));
    }

    [Fact]
    public async Task Duplicate_numbers_are_skipped_while_valid_rows_import()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var response = await Upload(
            client,
            Csv(
                "09/15/2026,200 Way,100 St,09:15,TRIP-DUP,MED-1,VALID,First,Passenger,Phoenix,Mesa,N\n"
                    + "09/15/2026,200 Way,100 St,09:15,TRIP-DUP,MED-2,VALID,Second,Passenger,Phoenix,Mesa,N\n"
                    + "09/15/2026,200 Way,100 St,09:15,TRIP-GOOD,MED-3,VALID,Good,Passenger,Phoenix,Mesa,N"
            )
        );
        var result = await response.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.ReadyCount);
        Assert.Equal(2, result.NeedsAttentionCount);
    }

    [Fact]
    public async Task Missing_required_column_returns_an_actionable_problem_without_mutation()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var response = await Upload(
            client,
            "trips.csv",
            System.Text.Encoding.UTF8.GetBytes("Trip Number\nTRIP-1")
        );
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(result);
        Assert.Equal(0, result.ReadyCount);
        Assert.Equal(0, result.NeedsAttentionCount);
        Assert.Contains(
            result.Problems,
            problem => problem.Message.Contains("missing required columns", StringComparison.OrdinalIgnoreCase)
        );
        var missingColumns = Assert.Single(result.Problems);
        Assert.Equal("manifest.missingColumns", missingColumns.Code);
        Assert.Contains("Medicaid Number", missingColumns.Parameters!["columns"]);
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Trips.IgnoreQueryFilters().ToListAsync());
    }

    private static Task<HttpResponseMessage> Upload(HttpClient client, string csv) =>
        Upload(client, "trips.csv", System.Text.Encoding.UTF8.GetBytes(csv));

    private static Task<HttpResponseMessage> Upload(HttpClient client, string name, byte[] content) =>
        client.PostAsync(
            "/api/trips/import",
            new MultipartFormDataContent { { new ByteArrayContent(content), "file", name } }
        );

    private static string Csv(
        string row =
            "09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-100,MED-100,VALID,Synthetic,Passenger,Phoenix,Mesa,N"
    ) =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag,Trip Type\n"
        + string.Join('\n', row.Split('\n').Select(value => value + ",T"));

    private static string Manifest(params string[] rows) =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag,Trip Type\n"
        + string.Join('\n', rows);

    private static string Outbound(string tripNumber) =>
        $"09/15/2026,200 Clinic Ave,100 Home St,09:15,{tripNumber},MED-100,VALID,Synthetic,Passenger,Phoenix,Mesa,N,T";

    private static string Inbound(string tripNumber) =>
        $"09/15/2026,100 Home St,200 Clinic Ave,13:30,{tripNumber},MED-100,VALID,Synthetic,Passenger,Mesa,Phoenix,N,F";

    private static byte[] Xlsx()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Trips");
        var headers = Csv().Split('\n')[0].Split(',');
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
        var row = Csv().Split('\n')[1].Split(',');
        for (var i = 0; i < row.Length; i++)
            sheet.Cell(2, i + 1).Value = row[i];
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private sealed record ImportTripsResponse(
        int ReadyCount,
        int NeedsAttentionCount,
        List<TripImportProblem> Problems
    );

    private sealed record TripImportProblem(
        int? RowNumber,
        string? TripNumber,
        string? Field,
        string Message,
        string Code,
        Dictionary<string, string>? Parameters
    );
}
