namespace Mdsweep.Application.Dispatch;

public sealed record ScheduledPickupTimeChangeResponse(
    long Sequence,
    TimeOnly ScheduledPickupTime,
    DateTimeOffset ChangedAt,
    string ChangedBy
);
