namespace Mdsweep.Application.Dispatch;

public sealed record ResetDriverAccess(
    Guid DriverId,
    ResetDriverAccessRequest Request
);
