using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.TripImports.Events;

namespace Mdsweep.Domain.TripImports;

public sealed class TripImportAggregate : AggregateRoot<Guid>, ITenanted
{
    private readonly List<TripImportRow> _rows = [];

    private TripImportAggregate()
        : base(default) { }

    private TripImportAggregate(Guid id, string fileName, string contentFingerprint)
        : base(id)
    {
        FileName = fileName;
        ContentFingerprint = contentFingerprint;
    }

    // Stamped and filtered by Wolverine's conjoined-tenancy integration.
    public string? TenantId { get; set; }
    public string FileName { get; private set; } = null!;
    public string ContentFingerprint { get; private set; } = null!;
    public TripImportStatus Status { get; private set; } = TripImportStatus.Previewed;
    public Instant? AppliedAt { get; private set; }
    public IReadOnlyCollection<TripImportRow> Rows => _rows;

    public static TripImportAggregate Create(
        string fileName,
        string contentFingerprint,
        IEnumerable<TripImportRow> rows
    )
    {
        Guard.Against.NullOrWhiteSpace(fileName, nameof(fileName));
        Guard.Against.NullOrWhiteSpace(contentFingerprint, nameof(contentFingerprint));
        Guard.Against.Null(rows, nameof(rows));

        var import = new TripImportAggregate(Guid.CreateVersion7(), fileName, contentFingerprint);
        import._rows.AddRange(rows);
        import.AddDomainEvent(new TripImportPreviewedDomainEvent(import.Id));
        return import;
    }

    public void MarkApplied(Instant appliedAt)
    {
        Guard.Against.Invalid(Status is TripImportStatus.Applied, "This trip import has already been applied.");
        Status = TripImportStatus.Applied;
        AppliedAt = appliedAt;
        AddDomainEvent(new TripImportAppliedDomainEvent(Id, appliedAt));
    }
}

public sealed class TripImportRow
{
    private TripImportRow() { }

    public TripImportRow(
        int rowNumber,
        string? tripNumber,
        string? brokerMemberId,
        string? firstName,
        string? lastName,
        DateOnly? serviceDate,
        LocalTime? appointmentTime,
        string? pickupAddress,
        string? pickupCity,
        string? dropoffAddress,
        string? dropoffCity,
        string? brokerStatus,
        bool isWillCall,
        TripImportRowDisposition disposition,
        IReadOnlyList<string> messages
    )
    {
        Id = Guid.CreateVersion7();
        RowNumber = rowNumber;
        TripNumber = tripNumber;
        BrokerMemberId = brokerMemberId;
        FirstName = firstName;
        LastName = lastName;
        ServiceDate = serviceDate;
        AppointmentTime = appointmentTime;
        PickupAddress = pickupAddress;
        PickupCity = pickupCity;
        DropoffAddress = dropoffAddress;
        DropoffCity = dropoffCity;
        BrokerStatus = brokerStatus;
        IsWillCall = isWillCall;
        Disposition = disposition;
        MessagesJson = System.Text.Json.JsonSerializer.Serialize(messages);
    }

    public Guid Id { get; private set; }
    public Guid TripImportId { get; private set; }
    public int RowNumber { get; private set; }
    public string? TripNumber { get; private set; }
    public string? BrokerMemberId { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public DateOnly? ServiceDate { get; private set; }
    public LocalTime? AppointmentTime { get; private set; }
    public string? PickupAddress { get; private set; }
    public string? PickupCity { get; private set; }
    public string? DropoffAddress { get; private set; }
    public string? DropoffCity { get; private set; }
    public string? BrokerStatus { get; private set; }
    public bool IsWillCall { get; private set; }
    public TripImportRowDisposition Disposition { get; private set; }
    public string MessagesJson { get; private set; } = "[]";
    public Guid? AppliedTripId { get; private set; }

    public IReadOnlyList<string> Messages =>
        System.Text.Json.JsonSerializer.Deserialize<string[]>(MessagesJson) ?? [];

    public void MarkApplied(Guid tripId) => AppliedTripId = tripId;
}

public enum TripImportStatus { Previewed, Applied }
public enum TripImportRowDisposition { Ready, Warning, Blocked }
