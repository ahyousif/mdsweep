using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Vehicles;

public sealed class VehicleAggregate : AggregateRoot<Guid>, ITenanted
{
    public const int MaxDisplayLabelLength = 100;

    private VehicleAggregate()
        : base(default) { }

    private VehicleAggregate(Guid id)
        : base(id) { }

    public string? TenantId { get; set; }
    public string DisplayLabel { get; private set; } = null!;
    public string Vin { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    public static VehicleAggregate Create(string displayLabel, string vin)
    {
        var vehicle = new VehicleAggregate(Guid.CreateVersion7());
        vehicle.UpdateDetails(displayLabel, vin);
        return vehicle;
    }

    public void UpdateDetails(string displayLabel, string vin)
    {
        Guard.Against.NullOrWhiteSpace(displayLabel, nameof(displayLabel));
        Guard.Against.NullOrWhiteSpace(vin, nameof(vin));
        DisplayLabel = displayLabel;
        Vin = vin;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
