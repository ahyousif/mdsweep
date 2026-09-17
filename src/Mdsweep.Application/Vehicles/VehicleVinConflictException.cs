namespace Mdsweep.Application.Vehicles;

// Raised by persistence when concurrent writes race past the friendly duplicate check.
public sealed class VehicleVinConflictException(Exception innerException)
    : Exception("A vehicle with this VIN already exists in this tenant.", innerException);
