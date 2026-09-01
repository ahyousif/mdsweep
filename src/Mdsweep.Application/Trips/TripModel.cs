namespace Mdsweep.Application.Trips;

public sealed record TripModel(
    Guid Id,
    string BrokerTripNumber,
    LocalDate ServiceDate,
    LocalTime? AppointmentTime,
    string? BrokerStatus,
    bool IsWillCall,
    LocalTime? ScheduledPickupTime
);
