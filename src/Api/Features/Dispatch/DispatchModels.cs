namespace Mdsweep.Api.Features.Dispatch;

public sealed class TripSchedule
{
    public Guid TripId { get; init; }
    public TimeOnly ScheduledPickupTime { get; set; }
}

public sealed class ScheduledPickupTimeChange
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TripId { get; init; }
    public TimeOnly ScheduledPickupTime { get; init; }
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;
    public required string ChangedBy { get; init; }
}

public sealed record SetScheduledPickupTimeRequest(TimeOnly ScheduledPickupTime);

public sealed record ScheduledPickupTimeChangeResponse(
    TimeOnly ScheduledPickupTime,
    DateTimeOffset ChangedAt,
    string ChangedBy);
