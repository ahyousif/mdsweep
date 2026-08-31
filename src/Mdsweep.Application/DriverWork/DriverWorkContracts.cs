using Mdsweep.Domain.DriverWork;

namespace Mdsweep.Application.DriverWork;

public interface IDriverWorkClock
{
    DateTimeOffset UtcNow { get; }
}

public enum DriverWorkOutcome
{
    Success,
    Forbid,
    NotFound,
    BadRequest,
    Conflict,
}

public sealed record DriverWorkResult<T>(
    DriverWorkOutcome Outcome,
    T? Value = default,
    string? Message = null,
    string? Location = null
);

public sealed record ListDriverTrips(string TenantId, Guid UserId);

public sealed record GetDriverTripHistory(string TenantId, Guid UserId, string TripNumber);

public sealed record RecordDriverTripEvent(
    string TenantId,
    Guid UserId,
    string TripNumber,
    RecordDriverTripEventRequest Event
);

public sealed record SynchronizeDriverTripEvent(
    string TenantId,
    Guid UserId,
    SynchronizeDriverTripEventRequest Request
);

public sealed record ListDriverSyncConflicts(string TenantId);

public sealed record CorrectDriverTripEvent(
    string TenantId,
    Guid UserId,
    string TripNumber,
    Guid EventId,
    CorrectDriverTripEventRequest Correction
);

public sealed record RecordDriverTripEventRequest(
    DriverTripEventType Type,
    DateTimeOffset DeviceCapturedAt,
    bool? TripLogSigned,
    string? OutcomeReason,
    string? Note
);

public sealed record SynchronizeDriverTripEventRequest(
    Guid ActionId,
    string TripNumber,
    RecordDriverTripEventRequest Event
);

public sealed record CorrectDriverTripEventRequest(DateTimeOffset DeviceCapturedAt, string Reason);

public sealed record DriverTripEventCorrectionResponse(
    Guid Id,
    Guid DriverTripEventId,
    DateTimeOffset CorrectedDeviceCapturedAt,
    DateTimeOffset ReceivedAt,
    string Reason
);

public sealed record DriverTripSyncConflictResponse(
    Guid Id,
    string TripNumber,
    DriverTripEventType Type,
    DateTimeOffset DeviceCapturedAt,
    DateTimeOffset ReceivedAt,
    string Reason,
    bool? TripLogSigned,
    string? OutcomeReason,
    string? Note
);

public sealed record DriverTripEventResponse(
    Guid Id,
    DriverTripEventType Type,
    DateTimeOffset DeviceCapturedAt,
    DateTimeOffset ReceivedAt,
    string? OutcomeReason,
    string? Note,
    bool? TripLogSigned,
    IReadOnlyList<DriverTripEventCorrectionResponse>? Corrections = null
);

public sealed record DriverTripResponse(
    string TripNumber,
    string JourneyKey,
    DateOnly AppointmentDate,
    TimeOnly AppointmentTime,
    string MemberName,
    string PassengerType,
    string VehicleType,
    string PickupAddress,
    string PickupCity,
    string DeliveryAddress,
    string DeliveryCity,
    DriverTripEventType? LastEventType,
    DriverTripEventType? NextAction = null
);
