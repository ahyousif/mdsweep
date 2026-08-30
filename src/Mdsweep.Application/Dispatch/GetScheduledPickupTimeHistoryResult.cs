namespace Mdsweep.Application.Dispatch;

public sealed record GetScheduledPickupTimeHistoryResult(
    bool Found,
    IReadOnlyList<ScheduledPickupTimeChangeResponse> Changes
);
