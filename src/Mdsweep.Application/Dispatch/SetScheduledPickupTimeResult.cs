namespace Mdsweep.Application.Dispatch;

public sealed record SetScheduledPickupTimeResult(
    SetScheduledPickupTimeOutcome Outcome,
    TimeOnly ScheduledPickupTime
);
