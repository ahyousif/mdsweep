namespace Mdsweep.Application.Trips.Planning;

/// <summary>Payload for setting a Trip's planned pickup time.</summary>
public sealed record SetScheduledPickupTimeRequest(LocalTime ScheduledPickupTime);

public sealed record SetScheduledPickupTime(Guid UserId, string TripNumber, LocalTime ScheduledPickupTime);

public sealed record SetScheduledPickupTimeResult(
    SetScheduledPickupTimeOutcome Outcome,
    LocalTime ScheduledPickupTime
);

public enum SetScheduledPickupTimeOutcome { Updated, NotFound, Inactive }

public sealed record GetScheduledPickupTimeHistory(string TripNumber);

public sealed record GetScheduledPickupTimeHistoryResult(
    bool Found,
    IReadOnlyList<ScheduledPickupTimeChangeResponse> Changes
);

public sealed record GetServiceDayTrips(DateOnly ServiceDate);

public sealed record ScheduledPickupTimeChangeResponse(
    long Sequence,
    LocalTime ScheduledPickupTime,
    Instant ChangedAt,
    Guid ChangedByUserId
);

public sealed record ServiceDayTripResponse(
    string TripNumber,
    string JourneyKey,
    string MemberName,
    string PickupAddress,
    string PickupCity,
    string DeliveryAddress,
    string DeliveryCity,
    string PassengerType,
    string VehicleType,
    string BrokerStatus,
    TimeOnly AppointmentTime,
    LocalTime? ScheduledPickupTime,
    bool IsWillCall,
    bool IsActive
);
