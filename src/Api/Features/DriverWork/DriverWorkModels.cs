namespace Mdsweep.Api.Features.DriverWork;

public enum DriverTripEventType { ArrivedAtPickup, PickedUp, ArrivedAtDropOff, DroppedOff, CouldNotComplete }

public sealed class DriverTripEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
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
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DriverTripEventId { get; init; }
    public Guid CorrectedByDriverId { get; init; }
    public DateTimeOffset CorrectedDeviceCapturedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public required string Reason { get; init; }
}

public sealed class DriverTripSyncConflict
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProviderId { get; init; }
    public Guid DriverId { get; init; }
    public required string TripNumber { get; init; }
    public DriverTripEventType Type { get; init; }
    public DateTimeOffset DeviceCapturedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public required string Reason { get; init; }
}

public interface IDriverWorkClock { DateTimeOffset UtcNow { get; } }

public sealed class SystemDriverWorkClock : IDriverWorkClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
