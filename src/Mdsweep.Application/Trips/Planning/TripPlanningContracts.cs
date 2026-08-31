using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Trips.Planning;

public sealed record SetScheduledPickupTime(Guid TripId, LocalTime ScheduledPickupTime)
    : IRequest<SetScheduledPickupTimeResult>;

public sealed record SetScheduledPickupTimeResult(Guid TripId, LocalTime ScheduledPickupTime);

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
