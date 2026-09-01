using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips;

public sealed record TripModel(
    Guid Id,
    string BrokerTripNumber,
    LocalDate ServiceDate,
    LocalTime? AppointmentTime,
    string? BrokerStatus,
    bool IsWillCall,
    LocalTime? ScheduledPickupTime
)
{
    public static TripModel FromAggregate(TripAggregate trip) =>
        new(
            trip.Id,
            trip.BrokerTripNumber,
            LocalDate.FromDateOnly(trip.BrokerData.ServiceDate),
            trip.BrokerData.AppointmentTime,
            trip.BrokerData.BrokerStatus,
            trip.BrokerData.IsWillCall,
            trip.ScheduledPickupTime
        );
}
