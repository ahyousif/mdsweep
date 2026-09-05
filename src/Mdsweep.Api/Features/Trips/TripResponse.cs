using Mdsweep.Application.Trips;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Api.Features.Trips;

public sealed record TripResponse(
    Guid Id,
    string BrokerTripNumber,
    string PassengerFirstName,
    string PassengerLastName,
    string? BrokerMemberId,
    LocalDate ServiceDate,
    LocalTime? AppointmentTime,
    string? BrokerStatus,
    bool IsWillCall,
    PassengerMobilityRequirement MobilityRequirement,
    decimal? TripCost,
    decimal? TripMileage,
    LocalTime? ScheduledPickupTime,
    ScheduledPickupSource? ScheduledPickupSource,
    LocalTime? SuggestedPickupTime,
    int? EstimatedTravelMinutes,
    string PickupAddress,
    string PickupCity,
    string DropoffAddress,
    string DropoffCity
)
{
    public static TripResponse FromModel(TripModel model) =>
        new(
            model.Id,
            model.BrokerTripNumber,
            model.PassengerFirstName,
            model.PassengerLastName,
            model.BrokerMemberId,
            model.ServiceDate,
            model.AppointmentTime,
            model.BrokerStatus,
            model.IsWillCall,
            model.MobilityRequirement,
            model.TripCost,
            model.TripMileage,
            model.ScheduledPickupTime,
            model.ScheduledPickupSource,
            model.SuggestedPickupTime,
            model.EstimatedTravelMinutes,
            model.PickupAddress,
            model.PickupCity,
            model.DropoffAddress,
            model.DropoffCity
        );
}
