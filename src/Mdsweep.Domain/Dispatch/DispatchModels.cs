namespace Mdsweep.Domain.Dispatch;

public sealed class Driver
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string TenantId { get; init; }
    public Guid UserId { get; init; }
    public required string DisplayName { get; set; }
    public required string MtmDriverNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Vehicle
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string TenantId { get; init; }
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
    public Guid AssignedByUserId { get; init; }
    public DateTimeOffset AssignedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SupersededAt { get; set; }
}
