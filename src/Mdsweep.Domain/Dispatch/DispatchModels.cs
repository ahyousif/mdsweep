namespace Mdsweep.Domain.Dispatch;

public sealed class TripSchedule
{
    public Guid TripId { get; init; }
    public TimeOnly ScheduledPickupTime { get; set; }
}

public sealed class ScheduledPickupTimeChange
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public long Sequence { get; init; }
    public Guid TripId { get; init; }
    public TimeOnly ScheduledPickupTime { get; init; }
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;
    public required string ChangedBy { get; init; }
}

public sealed class Driver
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid ProviderId { get; init; }
    public Guid AppUserId { get; init; }
    public required string DisplayName { get; set; }
    public required string MtmDriverNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Vehicle
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid ProviderId { get; init; }
    public required string DisplayName { get; set; }
    public required string Vin { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TripAssignment
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TripId { get; init; }
    public Guid DriverId { get; init; }
    public Guid VehicleId { get; init; }
    public Guid AssignedByAppUserId { get; init; }
    public DateTimeOffset AssignedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SupersededAt { get; set; }
}
