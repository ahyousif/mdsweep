using Mdsweep.Application.Trips;

namespace Mdsweep.Api.Features.Trips;

public sealed record TripResponse(
    Guid Id,
    string BrokerTripNumber,
    LocalDate ServiceDate,
    LocalTime? AppointmentTime,
    string? BrokerStatus,
    bool IsWillCall,
    LocalTime? ScheduledPickupTime
)
{
    public static TripResponse FromModel(TripModel model) =>
        new(
            model.Id,
            model.BrokerTripNumber,
            model.ServiceDate,
            model.AppointmentTime,
            model.BrokerStatus,
            model.IsWillCall,
            model.ScheduledPickupTime
        );
}
