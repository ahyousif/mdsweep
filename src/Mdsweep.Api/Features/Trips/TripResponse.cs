using Mdsweep.Api.Common.Contracts;
using Mdsweep.Application.Trips;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Api.Features.Trips;

public sealed record TripResponse(
    Guid Id,
    string BrokerTripNumber,
    string PassengerFirstName,
    string PassengerLastName,
    string? MemberId,
    LocalDate ServiceDate,
    TripDirection Direction,
    string? BrokerStatus,
    bool IsWillCall,
    string? PassengerType,
    string? SpecialNeeds,
    decimal? TripCost,
    decimal? TripMileage,
    LocalTime? AppointmentTime,
    LocalTime? ReturnPickupTime,
    LocalTime? ScheduledPickupTime,
    LocalTime? CalculatedPickupTime,
    LocalTime? ManualPickupTime,
    int? EstimatedTravelMinutes,
    int? EstimatedDistanceMeters,
    AddressResponse Pickup,
    AddressResponse Dropoff
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
            model.Direction,
            model.BrokerStatus,
            model.IsWillCall,
            model.PassengerType,
            model.SpecialNeeds,
            model.TripCost,
            model.TripMileage,
            model.Direction == TripDirection.To ? model.Time : null,
            model.Direction == TripDirection.From && !model.IsWillCall ? model.Time : null,
            model.ScheduledPickupTime,
            model.CalculatedPickupTime,
            model.ManualPickupTime,
            model.EstimatedTravelMinutes,
            model.EstimatedDistanceMeters,
            new AddressResponse(model.PickupAddress, model.PickupCity, model.PickupState, model.PickupZip),
            new AddressResponse(model.DropoffAddress, model.DropoffCity, model.DropoffState, model.DropoffZip)
        );
}
