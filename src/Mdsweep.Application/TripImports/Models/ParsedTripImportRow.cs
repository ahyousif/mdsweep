namespace Mdsweep.Application.TripImports;

public sealed record ParsedTripImportRow(
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
    string? AppointmentDateValidationError,
    string? AppointmentTimeValidationError
);
