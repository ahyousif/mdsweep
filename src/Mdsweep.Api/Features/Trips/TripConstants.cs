namespace Mdsweep.Api.Features.Trips;

public static class TripConstants
{
    public const string Route = "/trips";
    public const string IdRoute = Route + "/{id:guid}";
    public const string ImportRoute = Route + "/import";
    public const string ScheduledPickupTimeRoute = IdRoute + "/scheduled-pickup-time";
    public const string ResetScheduledPickupTimeRoute = ScheduledPickupTimeRoute + "/reset";
    public const string CalculateScheduledPickupTimeRoute = ScheduledPickupTimeRoute + "/calculate";
    public const string Tag = "Trips";
}
