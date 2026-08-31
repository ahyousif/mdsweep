namespace Mdsweep.Domain.DriverWork;

public enum DriverTripEventType
{
    ArrivedAtPickup,
    PickedUp,
    ArrivedAtDropOff,
    DroppedOff,
    CouldNotComplete,
}

public sealed class DriverTripEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TripId { get; init; }
    public Guid DriverId { get; init; }
    public DriverTripEventType Type { get; init; }
    public DateTimeOffset DeviceCapturedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public string? OutcomeReason { get; init; }
    public string? Note { get; init; }
    public bool? TripLogSigned { get; init; }
}

public sealed class DriverTripEventCorrection
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid DriverTripEventId { get; init; }
    public Guid CorrectedByDriverId { get; init; }
    public DateTimeOffset CorrectedDeviceCapturedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public required string Reason { get; init; }
}

public sealed class DriverTripSyncConflict
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid ActionId { get; init; }
    public required string TenantId { get; init; }
    public Guid DriverId { get; init; }
    public required string TripNumber { get; init; }
    public DriverTripEventType Type { get; init; }
    public DateTimeOffset DeviceCapturedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public required string Reason { get; init; }
    public bool? TripLogSigned { get; init; }
    public string? OutcomeReason { get; init; }
    public string? Note { get; init; }
}
