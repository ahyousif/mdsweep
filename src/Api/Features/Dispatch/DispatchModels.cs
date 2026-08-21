namespace Mdsweep.Api.Features.Dispatch;

public sealed class TripSchedule
{
    public Guid TripId { get; init; }
    public TimeOnly ScheduledPickupTime { get; set; }
}

public sealed class ScheduledPickupTimeChange
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public long Sequence { get; init; }
    public Guid TripId { get; init; }
    public TimeOnly ScheduledPickupTime { get; init; }
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;
    public required string ChangedBy { get; init; }
}

public sealed class Driver
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProviderId { get; init; }
    public Guid AppUserId { get; init; }
    public required string DisplayName { get; set; }
    public required string MtmDriverNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Vehicle
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProviderId { get; init; }
    public required string DisplayName { get; set; }
    public required string Vin { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TripAssignment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TripId { get; init; }
    public Guid DriverId { get; init; }
    public Guid VehicleId { get; init; }
    public Guid AssignedByAppUserId { get; init; }
    public DateTimeOffset AssignedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SupersededAt { get; set; }
}

public sealed record SetScheduledPickupTimeRequest(TimeOnly ScheduledPickupTime);
public sealed record CreateDriverRequest(Guid AppUserId, string DisplayName, string MtmDriverNumber);
public sealed record CreateDriverAccessRequest(string Email, string TemporaryPassword, string DisplayName, string MtmDriverNumber);
public sealed record ResetDriverAccessRequest(string TemporaryPassword);
public sealed record DriverResponse(Guid Id, Guid AppUserId, string DisplayName, string MtmDriverNumber, bool IsActive);
public sealed record CreateVehicleRequest(string DisplayName, string Vin);
public sealed record VehicleResponse(Guid Id, string DisplayName, string Vin, bool IsActive);
public sealed record AssignTripRequest(Guid DriverId, Guid VehicleId);
public sealed record AssignmentResponse(Guid DriverId, Guid VehicleId, Guid AssignedByAppUserId, DateTimeOffset AssignedAt, DateTimeOffset? SupersededAt);

public sealed record ScheduledPickupTimeChangeResponse(
    long Sequence,
    TimeOnly ScheduledPickupTime,
    DateTimeOffset ChangedAt,
    string ChangedBy);

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
    bool IsActive);
