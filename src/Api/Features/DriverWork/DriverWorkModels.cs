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

public interface IDriverWorkClock { DateTimeOffset UtcNow { get; } }

public sealed class SystemDriverWorkClock : IDriverWorkClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
