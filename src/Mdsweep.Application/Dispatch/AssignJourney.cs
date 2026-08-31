namespace Mdsweep.Application.Dispatch;

public sealed record AssignJourney(
    Guid UserId,
    string JourneyKey,
    AssignTripRequest Request
);
