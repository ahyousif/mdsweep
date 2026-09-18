using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Import.Manifest;

public sealed record MtmManifestRow(
    int RowNumber,
    string TripNumber,
    string MemberId,
    string FirstName,
    string LastName,
    LocalDate? DateOfBirth,
    string? PhoneNumber,
    string? AlternatePhoneNumber,
    string? PassengerType,
    string? SpecialNeeds,
    LocalDate ServiceDate,
    LocalTime? Time,
    TripDirection Direction,
    bool IsWillCall,
    string PickupAddress,
    string PickupCity,
    string? PickupState,
    string? PickupZip,
    string DropoffAddress,
    string DropoffCity,
    string? DropoffState,
    string? DropoffZip,
    string? BrokerStatus,
    decimal? TripCost,
    decimal? TripMileage
);
