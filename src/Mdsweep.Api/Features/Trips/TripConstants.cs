namespace Mdsweep.Api.Features.Trips;

public static class TripConstants
{
    public const string Route = "/api/trips";
    public const string IdRoute = Route + "/{id:guid}";
    public const string ScheduledPickupTimeRoute = IdRoute + "/scheduled-pickup-time";
    public const string Tag = "Trips";
}
