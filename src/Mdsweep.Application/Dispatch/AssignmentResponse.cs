namespace Mdsweep.Application.Dispatch;

public sealed record AssignmentResponse(
    Guid DriverId,
    Guid VehicleId,
    Guid AssignedByAppUserId,
    DateTimeOffset AssignedAt,
    DateTimeOffset? SupersededAt
);
