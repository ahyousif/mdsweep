namespace Mdsweep.Infrastructure.Trips.Scheduling;

public sealed class GoogleRoutesOptions
{
    public const string SectionName = "GoogleRoutes";

    public string? ApiKey { get; init; }
}
