namespace Mdsweep.Infrastructure.Trips.Scheduling;

public sealed class TripSchedulingOptions
{
    public const string SectionName = "TripScheduling";

    public int SchedulingBufferMinutes { get; init; } = 15;
    public string PolicyVersion { get; init; } = "v1";
}
