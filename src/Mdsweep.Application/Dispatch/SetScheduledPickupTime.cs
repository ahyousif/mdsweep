namespace Mdsweep.Application.Dispatch;

public sealed record SetScheduledPickupTime(
    Guid ProviderId,
    Guid AppUserId,
    string TripNumber,
    TimeOnly ScheduledPickupTime
);
