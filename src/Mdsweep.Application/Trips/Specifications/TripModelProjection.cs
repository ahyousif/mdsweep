using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Specifications;

internal sealed class TripModelProjection : Specification<TripAggregate, TripModel>
{
    public static TripModelProjection Instance { get; } = new();

    private TripModelProjection()
    {
        Query.Select(trip => new TripModel(
            trip.Id,
            trip.BrokerTripNumber,
            LocalDate.FromDateOnly(trip.BrokerData.ServiceDate),
            trip.BrokerData.AppointmentTime,
            trip.BrokerData.BrokerStatus,
            trip.BrokerData.IsWillCall,
            trip.ScheduledPickupTime
        ));
    }
}
