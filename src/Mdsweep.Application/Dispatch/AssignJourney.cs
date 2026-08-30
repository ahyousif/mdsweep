namespace Mdsweep.Application.Dispatch;

public sealed record AssignJourney(
    Guid ProviderId,
    Guid AppUserId,
    string JourneyKey,
    AssignTripRequest Request
);
