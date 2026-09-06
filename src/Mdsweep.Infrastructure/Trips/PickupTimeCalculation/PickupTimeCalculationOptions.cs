namespace Mdsweep.Infrastructure.Trips.PickupTimeCalculation;

public sealed class PickupTimeCalculationOptions
{
    public const string SectionName = "PickupTimeCalculation";

    public int PickupTimeBufferMinutes { get; init; } = 15;
    public string PolicyVersion { get; init; } = "v1";
}
