namespace Mdsweep.Application.Dispatch;

public sealed record ResetDriverAccess(
    Guid ProviderId,
    Guid DriverId,
    ResetDriverAccessRequest Request
);
