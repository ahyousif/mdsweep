namespace Mdsweep.Domain.Trips;

public sealed record BrokerTripData(
    LocalDate ServiceDate,
    LocalTime? AppointmentTime,
    LocalTime? BrokerPickupTime,
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
    string? Status,
    string? PassengerType,
    string? SpecialNeeds,
    decimal? Cost,
    decimal? Mileage
);
