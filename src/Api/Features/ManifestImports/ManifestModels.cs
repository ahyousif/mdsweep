namespace Mdsweep.Api.Features.ManifestImports;

public sealed class Trip
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProviderId { get; init; }
    public required string TripNumber { get; init; }
    public required string JourneyKey { get; init; }
    public DateOnly AppointmentDate { get; internal set; }
    public TimeOnly AppointmentTime { get; internal set; }
    public string MemberFirstName { get; internal set; } = null!;
    public string MemberLastName { get; internal set; } = null!;
    public string PickupAddress { get; internal set; } = null!;
    public string PickupCity { get; internal set; } = null!;
    public string DeliveryAddress { get; internal set; } = null!;
    public string DeliveryCity { get; internal set; } = null!;
    public string? PassengerPhone { get; internal set; }
    public string PassengerType { get; internal set; } = null!;
    public string VehicleType { get; internal set; } = null!;
    public string BrokerStatus { get; internal set; } = null!;
    public bool IsWillCall { get; internal set; }
    public bool IsActive { get; internal set; }

    public void ReconcileBrokerFields(ManifestPreviewRow row)
    {
        AppointmentDate = row.AppointmentDate!.Value;
        AppointmentTime = row.AppointmentTime!.Value;
        MemberFirstName = row.MemberFirstName;
        MemberLastName = row.MemberLastName;
        PickupAddress = row.PickupAddress;
        PickupCity = row.PickupCity;
        DeliveryAddress = row.DeliveryAddress;
        DeliveryCity = row.DeliveryCity;
        PassengerPhone = row.PassengerPhone;
        PassengerType = row.PassengerType;
        VehicleType = row.VehicleType;
        BrokerStatus = row.BrokerStatus;
        IsWillCall = row.IsWillCall;
        IsActive = row.Disposition.IsActive();
    }

    public IReadOnlyList<string> BrokerDifferences(ManifestPreviewRow row)
    {
        var differences = new List<string>();
        AddIfDifferent(AppointmentDate == row.AppointmentDate, "appointment date");
        AddIfDifferent(AppointmentTime == row.AppointmentTime, "appointment time");
        AddIfDifferent(Same(MemberFirstName, row.MemberFirstName), "member first name");
        AddIfDifferent(Same(MemberLastName, row.MemberLastName), "member last name");
        AddIfDifferent(Same(PickupAddress, row.PickupAddress), "pickup address");
        AddIfDifferent(Same(PickupCity, row.PickupCity), "pickup city");
        AddIfDifferent(Same(DeliveryAddress, row.DeliveryAddress), "destination address");
        AddIfDifferent(Same(DeliveryCity, row.DeliveryCity), "destination city");
        AddIfDifferent(Same(PassengerPhone, row.PassengerPhone), "passenger phone");
        AddIfDifferent(Same(PassengerType, row.PassengerType), "passenger type");
        AddIfDifferent(Same(VehicleType, row.VehicleType), "vehicle type");
        AddIfDifferent(Same(BrokerStatus, row.BrokerStatus), "MTM status");
        AddIfDifferent(IsWillCall == row.IsWillCall, "will-call flag");
        return differences;

        void AddIfDifferent(bool same, string field)
        {
            if (!same) differences.Add(field);
        }
    }

    private static bool Same(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

public sealed class TripBrokerImport
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProviderId { get; init; }
    public Guid TripId { get; init; }
    public Guid ManifestPreviewId { get; init; }
    public DateTimeOffset ImportedAt { get; init; } = DateTimeOffset.UtcNow;
    public required string TripNumber { get; init; }
    public DateOnly AppointmentDate { get; init; }
    public TimeOnly AppointmentTime { get; init; }
    public required string PickupAddress { get; init; }
    public required string DeliveryAddress { get; init; }
    public required string BrokerStatus { get; init; }
}

public sealed class ManifestPreview
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProviderId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required string FileName { get; init; }
    public required string RowsJson { get; init; }
    public DateTimeOffset? AppliedAt { get; set; }
}

public enum ManifestRowDisposition { Ready, Warning, Blocked }
public enum ManifestBrokerChange { New, BrokerChanged, Unchanged, Blocked }

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
    string? PassengerPhone,
    string PassengerType,
    string VehicleType,
    string BrokerStatus,
    bool IsWillCall,
    ManifestBrokerChange BrokerChange = ManifestBrokerChange.New,
    bool HasProviderOverrides = false,
    bool IsActive = false);

public static class ManifestRowDispositionRules
{
    public static bool IsImportable(this ManifestRowDisposition disposition) =>
        disposition is ManifestRowDisposition.Ready or ManifestRowDisposition.Warning;

    public static bool IsActive(this ManifestRowDisposition disposition) =>
        disposition is ManifestRowDisposition.Ready;
}
