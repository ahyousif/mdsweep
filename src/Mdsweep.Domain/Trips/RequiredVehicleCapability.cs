namespace Mdsweep.Domain.Trips;

// A capability, not an imported vehicle type. Vehicle compatibility can grow
// from this vocabulary when the tenant vehicle model supports it.
public enum RequiredVehicleCapability
{
    StandardTransport,
    WheelchairAccessible,
}
