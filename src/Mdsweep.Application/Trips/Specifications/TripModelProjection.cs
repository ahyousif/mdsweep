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
            trip.Passenger.FirstName,
            trip.Passenger.LastName,
            trip.Passenger.BrokerMemberId,
            LocalDate.FromDateOnly(trip.BrokerData.ServiceDate),
            trip.BrokerData.AppointmentTime,
            trip.BrokerData.BrokerStatus,
            trip.BrokerData.IsWillCall,
            trip.BrokerData.MobilityRequirement,
            trip.BrokerData.TripCost,
            trip.BrokerData.TripMileage,
            trip.ScheduledPickupTime,
            trip.EstimatedTravelMinutes,
            trip.BrokerData.PickupAddress,
            trip.BrokerData.PickupCity,
            trip.BrokerData.DropoffAddress,
            trip.BrokerData.DropoffCity
        ));
    }
}
