namespace Mdsweep.Application.Dispatch;

public sealed record SetScheduledPickupTimeRequest(TimeOnly ScheduledPickupTime);

public sealed record SetScheduledPickupTime(
    Guid ProviderId,
    Guid AppUserId,
    string TripNumber,
    TimeOnly ScheduledPickupTime
);

public sealed record SetScheduledPickupTimeResult(
    SetScheduledPickupTimeOutcome Outcome,
    TimeOnly ScheduledPickupTime
);

public enum SetScheduledPickupTimeOutcome
{
    Updated,
    NotFound,
    Inactive,
}

public sealed record GetScheduledPickupTimeHistory(Guid ProviderId, string TripNumber);

public sealed record GetScheduledPickupTimeHistoryResult(
    bool Found,
    IReadOnlyList<ScheduledPickupTimeChangeResponse> Changes
);

public sealed record GetServiceDay(Guid ProviderId, DateOnly ServiceDate);

public enum DispatchManagementOutcome
{
    Success,
    NotFound,
    BadRequest,
    Conflict,
}

public sealed record DispatchManagementResult<T>(
    DispatchManagementOutcome Outcome,
    T? Value = default,
    string? Message = null,
    string? Location = null
);

public sealed record ListDrivers(Guid ProviderId);

public sealed record CreateDriver(Guid ProviderId, CreateDriverRequest Request);

public sealed record CreateDriverAccess(Guid ProviderId, CreateDriverAccessRequest Request);

public sealed record ResetDriverAccess(
    Guid ProviderId,
    Guid DriverId,
    ResetDriverAccessRequest Request
);

public sealed record DeactivateDriver(Guid ProviderId, Guid DriverId);

public sealed record ListVehicles(Guid ProviderId);

public sealed record CreateVehicle(Guid ProviderId, CreateVehicleRequest Request);

public sealed record DeactivateVehicle(Guid ProviderId, Guid VehicleId);

public sealed record AssignJourney(
    Guid ProviderId,
    Guid AppUserId,
    string JourneyKey,
    AssignTripRequest Request
);

public sealed record AssignSingleTrip(
    Guid ProviderId,
    Guid AppUserId,
    string TripNumber,
    AssignTripRequest Request
);

public sealed record GetAssignmentHistory(Guid ProviderId, string TripNumber);

public sealed record AssignmentMutationResponse(
    IReadOnlyList<string> AssignedTripNumbers,
    bool Warning
);

public sealed record CreateDriverRequest(
    Guid AppUserId,
    string DisplayName,
    string MtmDriverNumber
);

public sealed record CreateDriverAccessRequest(
    string Email,
    string TemporaryPassword,
    string DisplayName,
    string MtmDriverNumber
);

public sealed record ResetDriverAccessRequest(string TemporaryPassword);

public sealed record DriverResponse(
    Guid Id,
    Guid AppUserId,
    string DisplayName,
    string MtmDriverNumber,
    bool IsActive
);

public sealed record CreateVehicleRequest(string DisplayName, string Vin);

public sealed record VehicleResponse(Guid Id, string DisplayName, string Vin, bool IsActive);

public sealed record AssignTripRequest(Guid DriverId, Guid VehicleId);

public sealed record AssignmentResponse(
    Guid DriverId,
    Guid VehicleId,
    Guid AssignedByAppUserId,
    DateTimeOffset AssignedAt,
    DateTimeOffset? SupersededAt
);

public sealed record ScheduledPickupTimeChangeResponse(
    long Sequence,
    TimeOnly ScheduledPickupTime,
    DateTimeOffset ChangedAt,
    string ChangedBy
);

public sealed record ServiceDayTripResponse(
    string TripNumber,
    string JourneyKey,
    string MemberName,
    string PickupAddress,
    string PickupCity,
    string DeliveryAddress,
    string DeliveryCity,
    string PassengerType,
    string VehicleType,
    string BrokerStatus,
    TimeOnly AppointmentTime,
    TimeOnly? ScheduledPickupTime,
    bool IsWillCall,
    bool IsActive
);
