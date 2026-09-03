namespace Mdsweep.Domain.Trips;

public sealed record BrokerTripData(
    DateOnly ServiceDate,
    LocalTime? AppointmentTime,
    string PickupAddress,
    string PickupCity,
    string DropoffAddress,
    string DropoffCity,
    string? BrokerStatus,
    bool IsWillCall
);
