namespace Mdsweep.Api.Features.Vehicles;

public static class VehicleConstants
{
    public const string Tag = "Vehicles";
    public const string Route = "/vehicles";
    public const string IdRoute = Route + "/{id:guid}";
    public const string Active = IdRoute + "/active";
}
