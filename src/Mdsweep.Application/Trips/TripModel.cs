using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips;

public sealed record TripModel(
    Guid Id,
    string BrokerTripNumber,
    LocalTime? ScheduledPickupTime
)
{
    public static TripModel FromAggregate(TripAggregate trip) =>
        new(trip.Id, trip.BrokerTripNumber, trip.ScheduledPickupTime);
}
