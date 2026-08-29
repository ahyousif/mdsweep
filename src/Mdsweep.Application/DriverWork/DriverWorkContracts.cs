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

public sealed record ListDriverTrips(Guid ProviderId, Guid AppUserId);

public sealed record GetDriverTripHistory(Guid ProviderId, Guid AppUserId, string TripNumber);

public sealed record RecordDriverTripEvent(
    Guid ProviderId,
    Guid AppUserId,
    string TripNumber,
    RecordDriverTripEventRequest Event
);

public sealed record SynchronizeDriverTripEvent(
    Guid ProviderId,
    Guid AppUserId,
    SynchronizeDriverTripEventRequest Request
);

public sealed record ListDriverSyncConflicts(Guid ProviderId);

public sealed record CorrectDriverTripEvent(
    Guid ProviderId,
    Guid AppUserId,
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
    string? PassengerPhone,
    DriverTripEventType? LastEventType,
    DriverTripEventType? NextAction = null
);
