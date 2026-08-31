namespace Mdsweep.Application.Dispatch;

public sealed record SetScheduledPickupTimeRequest(TimeOnly ScheduledPickupTime);

public sealed record SetScheduledPickupTime(
    string TenantId,
    Guid UserId,
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

public sealed record GetScheduledPickupTimeHistory(string TenantId, string TripNumber);

public sealed record GetScheduledPickupTimeHistoryResult(
    bool Found,
    IReadOnlyList<ScheduledPickupTimeChangeResponse> Changes
);

public sealed record GetServiceDay(string TenantId, DateOnly ServiceDate);

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

public sealed record ListDrivers(string TenantId);

public sealed record CreateDriver(string TenantId, CreateDriverRequest Request);

public sealed record CreateDriverAccess(string TenantId, CreateDriverAccessRequest Request);

public sealed record ResetDriverAccess(
    string TenantId,
    Guid DriverId,
    ResetDriverAccessRequest Request
);

public sealed record DeactivateDriver(string TenantId, Guid DriverId);

public sealed record ListVehicles(string TenantId);

public sealed record CreateVehicle(string TenantId, CreateVehicleRequest Request);

public sealed record DeactivateVehicle(string TenantId, Guid VehicleId);

public sealed record AssignJourney(
    string TenantId,
    Guid UserId,
    string JourneyKey,
    AssignTripRequest Request
);

public sealed record AssignSingleTrip(
    string TenantId,
    Guid UserId,
    string TripNumber,
    AssignTripRequest Request
);

public sealed record GetAssignmentHistory(string TenantId, string TripNumber);

public sealed record AssignmentMutationResponse(
    IReadOnlyList<string> AssignedTripNumbers,
    bool Warning
);

public sealed record CreateDriverRequest(
    Guid UserId,
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
    Guid UserId,
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
    Guid AssignedByUserId,
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
