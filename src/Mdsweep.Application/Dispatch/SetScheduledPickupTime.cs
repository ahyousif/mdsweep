namespace Mdsweep.Application.Dispatch;

public sealed record SetScheduledPickupTime(
    Guid UserId,
    string TripNumber,
    TimeOnly ScheduledPickupTime
);
