namespace Mdsweep.Api.Features.Dispatch;

public sealed class TripSchedule
{
    public Guid TripId { get; init; }
    public TimeOnly ScheduledPickupTime { get; set; }
}

public sealed class ScheduledPickupTimeChange
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public long Sequence { get; init; }
    public Guid TripId { get; init; }
    public TimeOnly ScheduledPickupTime { get; init; }
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;
    public required string ChangedBy { get; init; }
}

public sealed record SetScheduledPickupTimeRequest(TimeOnly ScheduledPickupTime);

public sealed record ScheduledPickupTimeChangeResponse(
    long Sequence,
    TimeOnly ScheduledPickupTime,
    DateTimeOffset ChangedAt,
    string ChangedBy);

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
    TimeOnly? ScheduledPickupTime,
    bool IsWillCall,
    bool IsActive);
