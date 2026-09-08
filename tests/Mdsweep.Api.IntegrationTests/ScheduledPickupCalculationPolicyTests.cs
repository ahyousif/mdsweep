using Mdsweep.Domain.Trips;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ScheduledPickupCalculationPolicyTests
{
    [Fact]
    public void Manual_pickup_override_takes_precedence_over_the_calculated_pickup_time()
    {
        var trip = CreateTrip();
        var calculatedPickupTime = new LocalTime(9, 5);
        trip.UpdateCalculatedSchedule(calculatedPickupTime, "synthetic-fingerprint");
        trip.OverridePickupTime(new LocalTime(8, 55));

        Assert.Equal(new LocalTime(8, 55), trip.ScheduledPickupTime);
        Assert.Equal(calculatedPickupTime, trip.CalculatedPickupTime);
    }

    [Fact]
    public void Removing_a_manual_pickup_override_restores_the_calculated_pickup_time()
    {
        var trip = CreateTrip();
        var calculatedPickupTime = new LocalTime(9, 5);
        trip.UpdateCalculatedSchedule(calculatedPickupTime, "synthetic-fingerprint");
        trip.OverridePickupTime(new LocalTime(8, 55));

        trip.RemovePickupOverride();

        Assert.Equal(calculatedPickupTime, trip.ScheduledPickupTime);
    }

    private static TripAggregate CreateTrip() => TripAggregate.Create(Guid.CreateVersion7(), "TRIP-SCHEDULED", new BrokerTripData(
        new LocalDate(2026, 9, 15), new LocalTime(10, 0), TripDirection.To, false,
        "100 Sample St", "Phoenix", null, null, "200 Synthetic Way", "Mesa", null, null,
        "VALID", null, null, null, null));
}
