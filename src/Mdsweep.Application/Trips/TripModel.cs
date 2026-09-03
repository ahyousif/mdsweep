namespace Mdsweep.Application.Trips;

public sealed record TripModel(
    Guid Id,
    string BrokerTripNumber,
    LocalDate ServiceDate,
    LocalTime? AppointmentTime,
    string? BrokerStatus,
    bool IsWillCall,
    LocalTime? ScheduledPickupTime,
    string PickupAddress,
    string PickupCity,
    string DropoffAddress,
    string DropoffCity
);
