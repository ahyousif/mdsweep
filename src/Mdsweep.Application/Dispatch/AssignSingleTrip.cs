namespace Mdsweep.Application.Dispatch;

public sealed record AssignSingleTrip(
    Guid UserId,
    string TripNumber,
    AssignTripRequest Request
);
