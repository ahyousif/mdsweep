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
        var firstBrokerData = firstTrip.BrokerData;
        using var repeat = await Upload(client, Csv());
        var repeatResult = await repeat.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(repeatResult);
        Assert.Equal(1, repeatResult.ReadyCount);
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repeatedTrip = await db.Trips.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(firstTripId, repeatedTrip.Id);
        Assert.Equal(firstJourneyId, repeatedTrip.JourneyId);
        Assert.Equal(firstBrokerData, repeatedTrip.BrokerData);
        Assert.Single(await db.Journeys.IgnoreQueryFilters().ToListAsync());
        Assert.Equal(
            JourneyGroupingType.Automatic,
            (await db.Journeys.IgnoreQueryFilters().SingleAsync()).GroupingType
        );
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
        Assert.Equal(
            JourneyGroupingType.Automatic,
            (await db.Journeys.IgnoreQueryFilters().SingleAsync()).GroupingType
        );
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
    public async Task Import_creates_passenger_with_mtm_profile_data()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var response = await Upload(
            client,
            ProfileCsv("09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-PROFILE,MED-PROFILE,VALID,Synthetic,Passenger,Phoenix,Mesa,N,T,01/02/1980,555-0100,555-0199,Wheel Chair,Cannot Transfer,46")
        );
        response.EnsureSuccessStatusCode();

        await using var scope = Application.Services.CreateAsyncScope();
        var passenger = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Passengers.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("MED-PROFILE", passenger.BrokerMemberId);
        Assert.Equal("Synthetic", passenger.FirstName);
        Assert.Equal("Passenger", passenger.LastName);
        Assert.Equal(new LocalDate(1980, 1, 2), passenger.DateOfBirth);
        Assert.Equal("555-0100", passenger.PhoneNumber);
        Assert.Equal("555-0199", passenger.AlternatePhoneNumber);
        Assert.Equal("Wheel Chair", passenger.PassengerType);
        Assert.Equal("Cannot Transfer", passenger.SpecialNeeds);
    }

    [Fact]
    public async Task Reimport_updates_changed_mtm_passenger_fields()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(
            client,
            ProfileCsv("09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-PROFILE,MED-PROFILE,VALID,Synthetic,Passenger,Phoenix,Mesa,N,T,01/02/1980,555-0100,555-0199,Ambulatory,None,46")
        );
        first.EnsureSuccessStatusCode();
        using var repeat = await Upload(
            client,
            ProfileCsv("09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-PROFILE,MED-PROFILE,VALID,Updated,Member,Phoenix,Mesa,N,T,03/04/1981,555-0200,555-0299,Wheel Chair,Cannot Transfer,45")
        );
        repeat.EnsureSuccessStatusCode();

        await using var scope = Application.Services.CreateAsyncScope();
        var passenger = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Passengers.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Updated", passenger.FirstName);
        Assert.Equal("Member", passenger.LastName);
        Assert.Equal(new LocalDate(1981, 3, 4), passenger.DateOfBirth);
        Assert.Equal("555-0200", passenger.PhoneNumber);
        Assert.Equal("555-0299", passenger.AlternatePhoneNumber);
        Assert.Equal("Wheel Chair", passenger.PassengerType);
        Assert.Equal("Cannot Transfer", passenger.SpecialNeeds);
    }

    [Fact]
    public async Task Reimport_preserves_mdsweep_owned_passenger_notes()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, ProfileCsv("09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-NOTES,MED-NOTES,VALID,Synthetic,Passenger,Phoenix,Mesa,N,T,01/02/1980,555-0100,,None,,46"));
        first.EnsureSuccessStatusCode();

        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passenger = await db.Passengers.IgnoreQueryFilters().SingleAsync();
            passenger.UpdateNotes("Call from the east entrance.");
            await db.SaveChangesAsync();
        }

        using var repeat = await Upload(client, ProfileCsv("09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-NOTES,MED-NOTES,VALID,Synthetic,Passenger,Phoenix,Mesa,N,T,01/02/1980,555-0200,,Wheel Chair,Needs ramp,46"));
        repeat.EnsureSuccessStatusCode();

        await using var verification = Application.Services.CreateAsyncScope();
        var savedPassenger = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Passengers.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Call from the east entrance.", savedPassenger.Notes);
    }

    [Fact]
    public async Task Missing_optional_passenger_fields_do_not_block_import()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var response = await Upload(client, Csv());
        var result = await response.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.ReadyCount);

        await using var scope = Application.Services.CreateAsyncScope();
        var passenger = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Passengers.IgnoreQueryFilters().SingleAsync();
        Assert.Null(passenger.DateOfBirth);
        Assert.Null(passenger.PhoneNumber);
        Assert.Null(passenger.AlternatePhoneNumber);
        Assert.Null(passenger.PassengerType);
        Assert.Null(passenger.SpecialNeeds);
    }

    [Fact]
    public async Task Multiple_manifest_trips_for_one_member_share_a_passenger()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var response = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        response.EnsureSuccessStatusCode();

        await using var scope = Application.Services.CreateAsyncScope();
        var trips = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Trips.IgnoreQueryFilters().ToListAsync();
        Assert.Equal(2, trips.Count);
        Assert.Single(trips.Select(trip => trip.PassengerId).Distinct());
        Assert.Single(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Passengers.IgnoreQueryFilters().ToListAsync());
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
        Assert.Equal(new LocalDate(2026, 9, 16), trip.BrokerData.ServiceDate);
        Assert.Equal("101 St", trip.BrokerData.PickupAddress);
    }

    [Fact]
    public async Task Later_reciprocal_trip_joins_the_existing_automatic_singleton_journey()
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
        var journey = await verification
            .ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Journeys.IgnoreQueryFilters()
            .SingleAsync();
        Assert.Equal(JourneyGroupingType.Automatic, journey.GroupingType);
    }

    [Fact]
    public async Task Automatic_pair_splits_when_service_date_changes()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();
        var original = await JourneyIdsAsync();

        var changedRow = Outbound("1001").Replace("09/15/2026", "09/16/2026", StringComparison.Ordinal);
        using var changed = await Upload(client, Manifest(changedRow));
        changed.EnsureSuccessStatusCode();

        var split = await JourneyIdsAsync();
        Assert.NotEqual(split["1001"], split["1002"]);
        Assert.Equal(original["1002"], split["1002"]);

        using var repeat = await Upload(client, Manifest(changedRow));
        repeat.EnsureSuccessStatusCode();
        var repeated = await JourneyIdsAsync();
        Assert.Equal(split["1001"], repeated["1001"]);
        Assert.Equal(split["1002"], repeated["1002"]);
    }

    [Fact]
    public async Task Unchanged_pair_reimport_keeps_trip_and_journey_ids()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();
        await using var firstDb = CreateDbContext();
        var original = await firstDb.Trips.IgnoreQueryFilters()
            .ToDictionaryAsync(trip => trip.BrokerTripNumber, trip => (trip.Id, trip.JourneyId));

        using var repeat = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        repeat.EnsureSuccessStatusCode();

        await using var verification = CreateDbContext();
        var repeated = await verification.Trips.IgnoreQueryFilters()
            .ToDictionaryAsync(trip => trip.BrokerTripNumber, trip => (trip.Id, trip.JourneyId));
        Assert.Equal(2, repeated.Count);
        Assert.Equal(original["1001"], repeated["1001"]);
        Assert.Equal(original["1002"], repeated["1002"]);
        Assert.Single(await verification.Journeys.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Automatic_pair_splits_when_route_changes()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();

        using var changed = await Upload(
            client,
            Manifest(Outbound("1001").Replace("100 Home St", "101 Other Home St", StringComparison.Ordinal))
        );
        changed.EnsureSuccessStatusCode();

        var journeys = await JourneyIdsAsync();
        Assert.NotEqual(journeys["1001"], journeys["1002"]);
    }

    [Fact]
    public async Task Non_grouping_change_preserves_automatic_journey()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();
        var original = await JourneyIdsAsync();

        using var changed = await Upload(
            client,
            Manifest(Outbound("1001").Replace(",VALID,", ",UPDATED,", StringComparison.Ordinal))
        );
        changed.EnsureSuccessStatusCode();

        var after = await JourneyIdsAsync();
        Assert.Equal(original["1001"], after["1001"]);
        Assert.Equal(original["1002"], after["1002"]);
    }

    [Fact]
    public async Task Reciprocal_grouping_changes_preserve_a_still_valid_automatic_pair()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();
        var original = await JourneyIdsAsync();

        var changedRows = new[] { Outbound("1001"), Inbound("1002") }
            .Select(row => row.Replace("09/15/2026", "09/16/2026", StringComparison.Ordinal))
            .ToArray();
        using var changed = await Upload(client, Manifest(changedRows));
        changed.EnsureSuccessStatusCode();

        var after = await JourneyIdsAsync();
        Assert.Equal(original["1001"], after["1001"]);
        Assert.Equal(original["1002"], after["1002"]);
    }

    [Fact]
    public async Task Manual_journey_is_not_regrouped_after_manifest_change()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();
        var original = await JourneyIdsAsync();
        await using (var db = CreateDbContext())
        {
            var journey = await db.Journeys.IgnoreQueryFilters().SingleAsync();
            journey.MarkManual();
            await db.SaveChangesAsync();
        }

        using var changed = await Upload(
            client,
            Manifest(Outbound("1001").Replace("09/15/2026", "09/16/2026", StringComparison.Ordinal))
        );
        changed.EnsureSuccessStatusCode();

        var after = await JourneyIdsAsync();
        Assert.Equal(original["1001"], after["1001"]);
        Assert.Equal(original["1002"], after["1002"]);
    }

    [Fact]
    public async Task Invalidated_trip_can_join_existing_automatic_singleton()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();
        var singletonRow = Inbound("1003")
            .Replace("09/15/2026", "09/16/2026", StringComparison.Ordinal)
            .Replace("100 Home St", "101 Other Home St", StringComparison.Ordinal);
        using var singleton = await Upload(client, Manifest(singletonRow));
        singleton.EnsureSuccessStatusCode();
        var before = await JourneyIdsAsync();

        var changedRow = Outbound("1001")
            .Replace("09/15/2026", "09/16/2026", StringComparison.Ordinal)
            .Replace("100 Home St", "101 Other Home St", StringComparison.Ordinal);
        using var changed = await Upload(client, Manifest(changedRow));
        changed.EnsureSuccessStatusCode();

        var after = await JourneyIdsAsync();
        Assert.Equal(before["1003"], after["1001"]);
        Assert.Equal(before["1002"], after["1002"]);
    }

    [Fact]
    public async Task Ambiguous_changed_trip_matches_remain_separate()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();
        var candidateRows = new[] { Inbound("1003"), Inbound("1004") }
            .Select(row => row.Replace("09/15/2026", "09/16/2026", StringComparison.Ordinal))
            .ToArray();
        using var candidates = await Upload(client, Manifest(candidateRows));
        candidates.EnsureSuccessStatusCode();

        using var changed = await Upload(
            client,
            Manifest(Outbound("1001").Replace("09/15/2026", "09/16/2026", StringComparison.Ordinal))
        );
        changed.EnsureSuccessStatusCode();

        var journeys = await JourneyIdsAsync();
        Assert.Equal(4, journeys.Values.Distinct().Count());
    }

    [Fact]
    public async Task Regrouping_does_not_leave_empty_automatic_journeys()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001")));
        first.EnsureSuccessStatusCode();
        using var second = await Upload(
            client,
            Manifest(Inbound("1002").Replace("09/15/2026", "09/16/2026", StringComparison.Ordinal))
        );
        second.EnsureSuccessStatusCode();
        var before = await JourneyIdsAsync();

        using var changed = await Upload(
            client,
            Manifest(Outbound("1001").Replace("09/15/2026", "09/16/2026", StringComparison.Ordinal))
        );
        changed.EnsureSuccessStatusCode();

        var after = await JourneyIdsAsync();
        Assert.Equal(after["1001"], after["1002"]);
        Assert.Equal(before["1002"], after["1002"]);
        await using var db = CreateDbContext();
        Assert.Single(await db.Journeys.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Later_reciprocal_trip_does_not_join_a_manual_singleton()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001")));
        first.EnsureSuccessStatusCode();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;
        Guid originalJourneyId;
        await using (var db = new ApplicationDbContext(options))
        {
            var journey = await db.Journeys.IgnoreQueryFilters().SingleAsync();
            originalJourneyId = journey.Id;
            journey.MarkManual();
            await db.SaveChangesAsync();
        }

        using var later = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        later.EnsureSuccessStatusCode();

        await using var verification = new ApplicationDbContext(options);
        var trips = await verification.Trips.IgnoreQueryFilters().OrderBy(trip => trip.BrokerTripNumber).ToListAsync();
        Assert.Equal(2, trips.Count);
        Assert.Equal(originalJourneyId, trips[0].JourneyId);
        Assert.NotEqual(originalJourneyId, trips[1].JourneyId);
        Assert.Equal(
            JourneyGroupingType.Automatic,
            (
                await verification
                    .Journeys.IgnoreQueryFilters()
                    .SingleAsync(journey => journey.Id == trips[1].JourneyId)
            ).GroupingType
        );
    }

    [Fact]
    public async Task Moving_a_trip_marks_both_journeys_manual_and_protects_the_remaining_source()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;
        Guid sourceJourneyId;
        Guid destinationJourneyId;
        await using (var db = new ApplicationDbContext(options))
        {
            var trips = await db.Trips.IgnoreQueryFilters().OrderBy(trip => trip.BrokerTripNumber).ToListAsync();
            var source = await db.Journeys.IgnoreQueryFilters().SingleAsync();
            var destination = JourneyAggregate.Create(JourneyGroupingType.Automatic);
            destination.TenantId = source.TenantId;
            db.Journeys.Add(destination);
            var unrelated = TripAggregate.Create(
                destination.Id,
                trips[0].PassengerId,
                "2000",
                trips[1].BrokerData with
                {
                    PickupAddress = "Other Synthetic Location",
                }
            );
            unrelated.TenantId = source.TenantId;
            db.Trips.Add(unrelated);
            trips[1].ChangeJourney(source, destination);
            sourceJourneyId = source.Id;
            destinationJourneyId = destination.Id;
            await db.SaveChangesAsync();
        }

        using var later = await Upload(client, Manifest(Outbound("1001"), Inbound("1002"), Inbound("1003")));
        later.EnsureSuccessStatusCode();

        await using var verification = new ApplicationDbContext(options);
        var stored = await verification.Trips.IgnoreQueryFilters().OrderBy(trip => trip.BrokerTripNumber).ToListAsync();
        Assert.Equal(4, stored.Count);
        Assert.Equal(sourceJourneyId, stored[0].JourneyId);
        Assert.Equal(destinationJourneyId, stored[1].JourneyId);
        Assert.NotEqual(sourceJourneyId, stored[2].JourneyId);
        Assert.NotEqual(destinationJourneyId, stored[2].JourneyId);
        var journeys = await verification.Journeys.IgnoreQueryFilters().ToDictionaryAsync(journey => journey.Id);
        Assert.Equal(JourneyGroupingType.Manual, journeys[sourceJourneyId].GroupingType);
        Assert.Equal(JourneyGroupingType.Manual, journeys[destinationJourneyId].GroupingType);
    }

    [Fact]
    public async Task Reimport_preserves_dispatcher_changed_journey_membership()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001")));
        first.EnsureSuccessStatusCode();

        Guid manualJourneyId;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using (var db = new ApplicationDbContext(options))
        {
            var trip = await db.Trips.IgnoreQueryFilters().SingleAsync();
            var sourceJourney = await db.Journeys.IgnoreQueryFilters().SingleAsync();
            var manualJourney = JourneyAggregate.Create(JourneyGroupingType.Manual);
            manualJourney.TenantId = trip.TenantId;
            db.Journeys.Add(manualJourney);
            trip.ChangeJourney(sourceJourney, manualJourney);
            await db.SaveChangesAsync();
            manualJourneyId = manualJourney.Id;
        }

        using var repeat = await Upload(
            client,
            Manifest(
                "09/15/2026,200 Clinic Ave,101 Home St,09:15,1001,MED-100,VALID,Synthetic,Passenger,Phoenix,Mesa,N,T"
            )
        );
        repeat.EnsureSuccessStatusCode();

        await using var verification = Application.Services.CreateAsyncScope();
        var stored = await verification
            .ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Trips.IgnoreQueryFilters()
            .SingleAsync();
        Assert.Equal(manualJourneyId, stored.JourneyId);
        Assert.Equal("101 Home St", stored.BrokerData.PickupAddress);
    }

    [Fact]
    public async Task Later_manifest_missing_a_trip_does_not_delete_it()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Manifest(Outbound("1001"), Inbound("1002")));
        first.EnsureSuccessStatusCode();
        Guid omittedTripJourneyId;
        await using (var firstScope = Application.Services.CreateAsyncScope())
        {
            omittedTripJourneyId = (
                await firstScope
                    .ServiceProvider.GetRequiredService<ApplicationDbContext>()
                    .Trips.IgnoreQueryFilters()
                    .SingleAsync(trip => trip.BrokerTripNumber == "1002")
            ).JourneyId;
        }
        using var later = await Upload(client, Manifest(Outbound("1001")));
        later.EnsureSuccessStatusCode();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new ApplicationDbContext(options);
        Assert.Equal(2, await db.Trips.IgnoreQueryFilters().CountAsync());
        Assert.Equal(
            omittedTripJourneyId,
            (await db.Trips.IgnoreQueryFilters().SingleAsync(trip => trip.BrokerTripNumber == "1002")).JourneyId
        );
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
        var journey = JourneyAggregate.Create(JourneyGroupingType.Automatic);
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

    private ApplicationDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
                .UseSnakeCaseNamingConvention()
                .Options
        );

    private async Task<Dictionary<string, Guid>> JourneyIdsAsync()
    {
        await using var db = CreateDbContext();
        return await db.Trips.IgnoreQueryFilters().ToDictionaryAsync(trip => trip.BrokerTripNumber, trip => trip.JourneyId);
    }

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

    private static string ProfileCsv(string row) =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag,Trip Type,Date of Birth,Member's Phone Number,Member's Alt Phone,Passenger Type,Special Needs,Member's Age\n"
        + row;

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
