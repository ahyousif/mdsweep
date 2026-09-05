using Mdsweep.Domain.Trips;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ScheduledPickupCalculationPolicyTests
{
    [Fact]
    public void Calculated_pickup_rounds_earlier_and_a_dispatcher_override_is_preserved()
    {
        var trip = TripAggregate.Create(
            Guid.CreateVersion7(),
            "TRIP-SCHEDULED",
            new BrokerTripData(
                new DateOnly(2026, 9, 15), new LocalTime(10, 0), "100 Sample St", "Phoenix", "200 Synthetic Way", "Mesa",
                "VALID", false, PassengerMobilityRequirement.Ambulatory, null, null, null));
        var suggestion = ScheduledPickupCalculationPolicy.Calculate(new LocalTime(10, 0), TimeSpan.FromMinutes(37), 15);

        Assert.Equal(new LocalTime(9, 5), suggestion);
        trip.ApplyCalculatedPickupTime(suggestion, 37, "synthetic-fingerprint");
        Assert.Equal(suggestion, trip.ScheduledPickupTime);
        Assert.Equal(ScheduledPickupSource.Calculated, trip.ScheduledPickupSource);

        trip.SetScheduledPickupTime(new LocalTime(8, 55));
        trip.ApplyCalculatedPickupTime(new LocalTime(9, 0), 40, "changed-synthetic-fingerprint");
        Assert.Equal(new LocalTime(8, 55), trip.ScheduledPickupTime);
        Assert.True(trip.ResetScheduledPickupToCalculated());
        Assert.Equal(new LocalTime(9, 0), trip.ScheduledPickupTime);
    }
}
