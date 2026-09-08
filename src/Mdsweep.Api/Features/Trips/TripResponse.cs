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
    LocalTime? Time,
    TripDirection Direction,
    string? BrokerStatus,
    bool IsWillCall,
    string? PassengerType,
    string? SpecialNeeds,
    decimal? TripCost,
    decimal? TripMileage,
    LocalTime? ScheduledPickupTime,
    LocalTime? CalculatedPickupTime,
    LocalTime? ManualPickupTime,
    string PickupAddress,
    string PickupCity,
    string? PickupState,
    string? PickupZip,
    string DropoffAddress,
    string DropoffCity,
    string? DropoffState,
    string? DropoffZip
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
            model.Time,
            model.Direction,
            model.BrokerStatus,
            model.IsWillCall,
            model.PassengerType,
            model.SpecialNeeds,
            model.TripCost,
            model.TripMileage,
            model.ScheduledPickupTime,
            model.CalculatedPickupTime,
            model.ManualPickupTime,
            model.PickupAddress,
            model.PickupCity,
            model.PickupState,
            model.PickupZip,
            model.DropoffAddress,
            model.DropoffCity,
            model.DropoffState,
            model.DropoffZip
        );
}
