using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;

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
        Guard.Against.Invalid(
            displayLabel.Length > MaxDisplayLabelLength,
            "Display label must be 100 characters or fewer."
        );
        Guard.Against.NullOrWhiteSpace(vin, nameof(vin));
        Guard.Against.Invalid(
            vin.Length != 17
                || vin.Any(character =>
                    !(character is >= 'A' and <= 'Z' or >= '0' and <= '9') || character is 'I' or 'O' or 'Q'
                ),
            "VIN must contain 17 uppercase letters or digits, excluding I, O and Q."
        );
        DisplayLabel = displayLabel;
        Vin = vin;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
