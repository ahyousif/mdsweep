namespace Mdsweep.Application.Dispatch;

public sealed record AssignmentMutationResponse(
    IReadOnlyList<string> AssignedTripNumbers,
    bool Warning
);
