namespace Mdsweep.Infrastructure.Trips.Routing;

public sealed class GoogleRoutesOptions
{
    public const string SectionName = "GoogleRoutes";

    public string? ApiKey { get; init; }
}
