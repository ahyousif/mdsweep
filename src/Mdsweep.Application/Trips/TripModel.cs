using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips;

public sealed record TripModel(
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
    int? EstimatedTravelMinutes,
    string PickupAddress,
    string PickupCity,
    string DropoffAddress,
    string DropoffCity
);
