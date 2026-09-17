namespace Mdsweep.Application.Vehicles;

public static class VehicleErrors
{
    public static ValidationError DuplicateVin() =>
        new("vin", "A vehicle with this VIN already exists in this tenant. Edit or reactivate the existing vehicle.")
        {
            ErrorCode = "vehicleVinExists",
        };
}
