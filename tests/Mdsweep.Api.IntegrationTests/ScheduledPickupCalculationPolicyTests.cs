using Mdsweep.Domain.Trips;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ScheduledPickupCalculationPolicyTests
{
    [Fact]
    public void Pickup_is_rounded_earlier_and_can_be_edited()
    {
        var trip = TripAggregate.Create(
            Guid.CreateVersion7(),
            "TRIP-SCHEDULED",
            new BrokerTripData(
                new DateOnly(2026, 9, 15), new LocalTime(10, 0), "100 Sample St", "Phoenix", "200 Synthetic Way", "Mesa",
                "VALID", false, PassengerMobilityRequirement.Ambulatory, null, null, null));
        var suggestion = ScheduledPickupCalculationPolicy.Calculate(new LocalTime(10, 0), TimeSpan.FromMinutes(37), 15);

        Assert.Equal(new LocalTime(9, 5), suggestion);
        trip.ApplyScheduledPickupTime(suggestion, 37, "synthetic-fingerprint");
        Assert.Equal(suggestion, trip.ScheduledPickupTime);

        trip.SetScheduledPickupTime(new LocalTime(8, 55));
        Assert.Equal(new LocalTime(8, 55), trip.ScheduledPickupTime);
    }

    [Fact]
    public void Previous_day_pickup_is_left_unset()
    {
        var pickup = ScheduledPickupCalculationPolicy.Calculate(
            new LocalTime(1, 0), TimeSpan.FromMinutes(75), 15);

        Assert.Null(pickup);
    }
}
