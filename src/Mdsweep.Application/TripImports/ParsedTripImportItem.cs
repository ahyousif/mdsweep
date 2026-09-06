using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.TripImports;

public sealed record ParsedTripImportItem(
    int RowNumber,
    string? TripNumber,
    string? BrokerMemberId,
    string? FirstName,
    string? LastName,
    DateOnly? ServiceDate,
    LocalTime? AppointmentTime,
    string? PickupAddress,
    string? PickupCity,
    string? DropoffAddress,
    string? DropoffCity,
    string? BrokerStatus,
    bool IsWillCall,
    PassengerMobilityRequirement? MobilityRequirement,
    string? RawImportedPassengerType,
    decimal? TripCost,
    decimal? TripMileage,
    string? AppointmentDateValidationError,
    string? AppointmentTimeValidationError,
    string? TripCostValidationError,
    string? TripMileageValidationError,
    string? PassengerTypeValidationError
);
