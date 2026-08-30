namespace Mdsweep.Application.Dispatch;

public sealed record CreateDriverRequest(
    Guid AppUserId,
    string DisplayName,
    string MtmDriverNumber
);
