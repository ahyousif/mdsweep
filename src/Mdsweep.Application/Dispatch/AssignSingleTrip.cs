namespace Mdsweep.Application.Dispatch;

public sealed record AssignSingleTrip(
    Guid ProviderId,
    Guid AppUserId,
    string TripNumber,
    AssignTripRequest Request
);
