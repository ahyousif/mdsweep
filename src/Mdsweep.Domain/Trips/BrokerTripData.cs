namespace Mdsweep.Domain.Trips;

public sealed record BrokerTripData(
    DateOnly ServiceDate,
    LocalTime? AppointmentTime,
    string PickupAddress,
    string PickupCity,
    string DropoffAddress,
    string DropoffCity,
    string? BrokerStatus,
    bool IsWillCall,
    PassengerMobilityRequirement MobilityRequirement,
    string? RawImportedPassengerType,
    decimal? TripCost,
    decimal? TripMileage
)
{
    public RequiredVehicleCapability RequiredVehicleCapability => MobilityRequirement switch
    {
        PassengerMobilityRequirement.ManualWheelchair or
        PassengerMobilityRequirement.ManualWheelchairCannotTransfer or
        PassengerMobilityRequirement.ElectricWheelchair =>
            global::Mdsweep.Domain.Trips.RequiredVehicleCapability.WheelchairAccessible,
        PassengerMobilityRequirement.Unknown => global::Mdsweep.Domain.Trips.RequiredVehicleCapability.Unknown,
        _ => global::Mdsweep.Domain.Trips.RequiredVehicleCapability.StandardTransport,
    };
}
