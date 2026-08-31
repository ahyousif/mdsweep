namespace Mdsweep.Api.Features.Dispatch;

internal static class DispatchRoutes
{
    public const string Drivers = "/api/drivers";
    public const string DriverAccess = Drivers + "/access";
    public const string ResetDriverAccess = Drivers + "/{driverId:guid}/reset-access";
    public const string DeactivateDriver = Drivers + "/{driverId:guid}/deactivate";
    public const string Vehicles = "/api/vehicles";
    public const string DeactivateVehicle = Vehicles + "/{vehicleId:guid}/deactivate";
    public const string AssignJourney = "/api/journeys/{journeyKey}/assignments";
    public const string AssignTrip = "/api/trips/{tripNumber}/assignments";
    public const string AssignmentHistory = AssignTrip;
    public const string ServiceDay = "/api/service-days/{serviceDate}/trips";
}
