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
            trip.BrokerData.ServiceDate,
            trip.BrokerData.Time,
            trip.BrokerData.Direction,
            trip.BrokerData.Status,
            trip.BrokerData.IsWillCall,
            trip.BrokerData.PassengerType,
            trip.BrokerData.SpecialNeeds,
            trip.BrokerData.Cost,
            trip.BrokerData.Mileage,
            trip.ScheduledPickupTime,
            trip.CalculatedPickupTime,
            trip.ManualPickupTime,
            trip.EstimatedTravelMinutes,
            trip.EstimatedDistanceMeters,
            trip.BrokerData.PickupAddress,
            trip.BrokerData.PickupCity,
            trip.BrokerData.PickupState,
            trip.BrokerData.PickupZip,
            trip.BrokerData.DropoffAddress,
            trip.BrokerData.DropoffCity,
            trip.BrokerData.DropoffState,
            trip.BrokerData.DropoffZip
        ));
    }
}
