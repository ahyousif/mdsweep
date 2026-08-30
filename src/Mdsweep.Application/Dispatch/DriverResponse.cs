namespace Mdsweep.Application.Dispatch;

public sealed record DriverResponse(
    Guid Id,
    Guid AppUserId,
    string DisplayName,
    string MtmDriverNumber,
    bool IsActive
);
