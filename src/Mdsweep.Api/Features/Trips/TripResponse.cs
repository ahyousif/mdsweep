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
    RequiredVehicleCapability RequiredVehicleCapability,
    decimal? TripCost,
    decimal? TripMileage,
    LocalTime? ScheduledPickupTime,
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
            model.RequiredVehicleCapability,
            model.TripCost,
            model.TripMileage,
            model.ScheduledPickupTime,
            model.PickupAddress,
            model.PickupCity,
            model.DropoffAddress,
            model.DropoffCity
        );
}
