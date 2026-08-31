namespace Mdsweep.Domain.Trips.Planning;

public sealed class TripSchedule
{
    public Guid TripId { get; init; }
    public LocalTime ScheduledPickupTime { get; set; }
}

public sealed class ScheduledPickupTimeChange
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public long Sequence { get; init; }
    public Guid TripId { get; init; }
    public LocalTime ScheduledPickupTime { get; init; }
    public Instant ChangedAt { get; init; }
    public Guid ChangedByUserId { get; init; }
}
