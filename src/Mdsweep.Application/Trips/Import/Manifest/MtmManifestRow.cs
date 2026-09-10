using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Import.Manifest;

public sealed record MtmManifestRow(
    int RowNumber,
    string TripNumber,
    string MemberId,
    string FirstName,
    string LastName,
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
    string? PassengerType,
    string? SpecialNeeds,
    decimal? TripCost,
    decimal? TripMileage
);
