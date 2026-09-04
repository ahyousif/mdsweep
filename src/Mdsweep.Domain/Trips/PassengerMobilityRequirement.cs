namespace Mdsweep.Domain.Trips;

// This is the operational transportation requirement supplied by the broker.
// It deliberately does not mirror a broker vehicle label.
public enum PassengerMobilityRequirement
{
    Ambulatory,
    Cane,
    ManualWheelchair,
    ManualWheelchairCannotTransfer,
    ElectricWheelchair,
}
