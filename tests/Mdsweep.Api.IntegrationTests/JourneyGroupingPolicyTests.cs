using Mdsweep.Application.Trips.Import;
using Mdsweep.Domain.Trips;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class JourneyGroupingPolicyTests
{
    private static readonly Guid PassengerOne = Guid.CreateVersion7();
    private static readonly Guid PassengerTwo = Guid.CreateVersion7();
    private static readonly LocalDate ServiceDate = new(2026, 9, 15);

    [Fact]
    public void One_new_trip_has_no_inferred_match()
    {
        var trip = Candidate("1001", PassengerOne, ServiceDate, TripDirection.To, "Home", "Clinic");

        var decision = JourneyGroupingPolicy.Group([trip], [])[trip.TripNumber];

        Assert.False(decision.HasMatch);
    }

    [Fact]
    public void Reciprocal_opposite_direction_trips_from_one_import_pair()
    {
        var outbound = Candidate("1001", PassengerOne, ServiceDate, TripDirection.To, " Home ", "Clinic");
        var inbound = Candidate("1002", PassengerOne, ServiceDate, TripDirection.From, "clinic", "HOME");

        var decisions = JourneyGroupingPolicy.Group([outbound, inbound], []);

        Assert.Equal(inbound.TripNumber, decisions[outbound.TripNumber].PairedNewTripNumber);
        Assert.Equal(outbound.TripNumber, decisions[inbound.TripNumber].PairedNewTripNumber);
    }

    [Fact]
    public void Same_passenger_and_date_with_unrelated_addresses_remain_separate()
    {
        var outbound = Candidate("1001", PassengerOne, ServiceDate, TripDirection.To, "Home", "Clinic");
        var inbound = Candidate("1002", PassengerOne, ServiceDate, TripDirection.From, "Pharmacy", "Home");

        Assert.All(
            JourneyGroupingPolicy.Group([outbound, inbound], []).Values,
            decision => Assert.False(decision.HasMatch)
        );
    }

    [Fact]
    public void Different_passengers_never_pair()
    {
        var outbound = Candidate("1001", PassengerOne, ServiceDate, TripDirection.To, "Home", "Clinic");
        var inbound = Candidate("1002", PassengerTwo, ServiceDate, TripDirection.From, "Clinic", "Home");

        Assert.All(
            JourneyGroupingPolicy.Group([outbound, inbound], []).Values,
            decision => Assert.False(decision.HasMatch)
        );
    }

    [Fact]
    public void Different_service_dates_never_pair()
    {
        var outbound = Candidate("1001", PassengerOne, ServiceDate, TripDirection.To, "Home", "Clinic");
        var inbound = Candidate("1002", PassengerOne, ServiceDate.PlusDays(1), TripDirection.From, "Clinic", "Home");

        Assert.All(
            JourneyGroupingPolicy.Group([outbound, inbound], []).Values,
            decision => Assert.False(decision.HasMatch)
        );
    }

    [Fact]
    public void Same_direction_never_pairs()
    {
        var first = Candidate("1001", PassengerOne, ServiceDate, TripDirection.To, "Home", "Clinic");
        var second = Candidate("1002", PassengerOne, ServiceDate, TripDirection.To, "Clinic", "Home");

        Assert.All(
            JourneyGroupingPolicy.Group([first, second], []).Values,
            decision => Assert.False(decision.HasMatch)
        );
    }

    [Fact]
    public void Ambiguous_reciprocal_candidates_all_remain_separate()
    {
        var outbound = Candidate("1001", PassengerOne, ServiceDate, TripDirection.To, "Home", "Clinic");
        var firstReturn = Candidate("1002", PassengerOne, ServiceDate, TripDirection.From, "Clinic", "Home");
        var secondReturn = Candidate("1003", PassengerOne, ServiceDate, TripDirection.From, "Clinic", "Home");

        Assert.All(
            JourneyGroupingPolicy.Group([outbound, firstReturn, secondReturn], []).Values,
            decision => Assert.False(decision.HasMatch)
        );
    }

    [Fact]
    public void Two_independent_reciprocal_pairs_form_two_pairings()
    {
        var trips = new[]
        {
            Candidate("1001", PassengerOne, ServiceDate, TripDirection.To, "Home", "Clinic"),
            Candidate("1002", PassengerOne, ServiceDate, TripDirection.From, "Clinic", "Home"),
            Candidate("2001", PassengerTwo, ServiceDate, TripDirection.To, "House", "Dentist"),
            Candidate("2002", PassengerTwo, ServiceDate, TripDirection.From, "Dentist", "House"),
        };

        var decisions = JourneyGroupingPolicy.Group(trips, []);

        Assert.Equal("1002", decisions["1001"].PairedNewTripNumber);
        Assert.Equal("2002", decisions["2001"].PairedNewTripNumber);
    }

    [Fact]
    public void Similar_broker_trip_numbers_have_no_grouping_effect()
    {
        var first = Candidate("TRIP-100-A", PassengerOne, ServiceDate, TripDirection.To, "Home", "Clinic");
        var second = Candidate("TRIP-100-B", PassengerOne, ServiceDate, TripDirection.From, "Other", "Elsewhere");

        Assert.All(
            JourneyGroupingPolicy.Group([first, second], []).Values,
            decision => Assert.False(decision.HasMatch)
        );
    }

    [Fact]
    public void New_trip_can_join_one_unambiguous_existing_single_leg_journey()
    {
        var journey = JourneyAggregate.Create();
        var existing = Existing(journey.Id, "1001", PassengerOne, TripDirection.To, "Home", "Clinic");
        var added = Candidate("1002", PassengerOne, ServiceDate, TripDirection.From, "Clinic", "Home");

        var decision = JourneyGroupingPolicy.Group([added], [existing])[added.TripNumber];

        Assert.Equal(journey.Id, decision.ExistingJourneyId);
    }

    [Fact]
    public void New_reciprocal_trip_does_not_join_an_already_paired_journey()
    {
        var journey = JourneyAggregate.Create();
        var outbound = Existing(journey.Id, "1001", PassengerOne, TripDirection.To, "Home", "Clinic");
        var inbound = Existing(journey.Id, "1002", PassengerOne, TripDirection.From, "Clinic", "Home");
        var added = Candidate("1003", PassengerOne, ServiceDate, TripDirection.From, "Clinic", "Home");

        var decision = JourneyGroupingPolicy.Group([added], [outbound, inbound])[added.TripNumber];

        Assert.False(decision.HasMatch);
    }

    private static JourneyGroupingCandidate Candidate(
        string tripNumber,
        Guid passengerId,
        LocalDate serviceDate,
        TripDirection direction,
        string pickup,
        string dropoff
    ) => new(tripNumber, passengerId, BrokerData(serviceDate, direction, pickup, dropoff));

    private static TripAggregate Existing(
        Guid journeyId,
        string tripNumber,
        Guid passengerId,
        TripDirection direction,
        string pickup,
        string dropoff
    ) => TripAggregate.Create(journeyId, passengerId, tripNumber, BrokerData(ServiceDate, direction, pickup, dropoff));

    private static BrokerTripData BrokerData(
        LocalDate serviceDate,
        TripDirection direction,
        string pickup,
        string dropoff
    ) =>
        new(
            serviceDate,
            direction == TripDirection.To ? new LocalTime(9, 0) : null,
            direction == TripDirection.From ? new LocalTime(13, 0) : null,
            direction,
            false,
            pickup,
            "Phoenix",
            null,
            null,
            dropoff,
            "Phoenix",
            null,
            null,
            "VALID",
            null,
            null,
            null,
            null
        );
}
