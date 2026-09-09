using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips;

public sealed record TripModel(
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
    int? EstimatedTravelMinutes,
    int? EstimatedDistanceMeters,
    string PickupAddress,
    string PickupCity,
    string? PickupState,
    string? PickupZip,
    string DropoffAddress,
    string DropoffCity,
    string? DropoffState,
    string? DropoffZip
);
