namespace Mdsweep.Api.Features.ManifestImports;

public sealed class Trip
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string TripNumber { get; init; }
    public required string JourneyKey { get; init; }
    public DateOnly AppointmentDate { get; init; }
    public TimeOnly AppointmentTime { get; init; }
    public required string MemberFirstName { get; init; }
    public required string MemberLastName { get; init; }
    public required string PickupAddress { get; init; }
    public required string PickupCity { get; init; }
    public required string DeliveryAddress { get; init; }
    public required string DeliveryCity { get; init; }
    public required string PassengerType { get; init; }
    public required string VehicleType { get; init; }
    public required string BrokerStatus { get; init; }
    public bool IsWillCall { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ManifestPreview
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required string FileName { get; init; }
    public required string RowsJson { get; init; }
    public DateTimeOffset? AppliedAt { get; set; }
}

public enum ManifestRowDisposition { Ready, Warning, Blocked }

public sealed record ManifestPreviewResponse(
    Guid PreviewId,
    int Ready,
    int Warning,
    int Blocked,
    IReadOnlyList<DateOnly> ServiceDates,
    IReadOnlyList<ManifestPreviewRow> Rows);

public sealed record ManifestPreviewRow(
    string TripNumber,
    ManifestRowDisposition Disposition,
    IReadOnlyList<string> Messages,
    DateOnly? AppointmentDate,
    TimeOnly? AppointmentTime,
    string MemberFirstName,
    string MemberLastName,
    string PickupAddress,
    string PickupCity,
    string DeliveryAddress,
    string DeliveryCity,
    string PassengerType,
    string VehicleType,
    string BrokerStatus,
    bool IsWillCall);

public static class ManifestRowDispositionRules
{
    public static bool IsImportable(this ManifestRowDisposition disposition) =>
        disposition is ManifestRowDisposition.Ready or ManifestRowDisposition.Warning;

    public static bool IsActive(this ManifestRowDisposition disposition) =>
        disposition is ManifestRowDisposition.Ready;
}

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
    bool IsWillCall,
    bool IsActive);
