using Mdsweep.Domain.Trips;

namespace Mdsweep.Infrastructure.TripImports.Parsing;

internal static class MtmPassengerMobilityMapper
{
    public static bool TryMap(
        string? passengerType,
        string? specialNeeds,
        out PassengerMobilityRequirement mobilityRequirement
    )
    {
        var sourceValue = passengerType?.ToUpperInvariant();
        var cannotTransfer = string.Equals(specialNeeds, "Cannot Transfer", StringComparison.OrdinalIgnoreCase);

        switch (sourceValue)
        {
            case null or "":
                mobilityRequirement = PassengerMobilityRequirement.Unknown;
                return true;
            case "AMBULATORY":
                mobilityRequirement = PassengerMobilityRequirement.Ambulatory;
                return true;
            case "CANE":
                mobilityRequirement = PassengerMobilityRequirement.Cane;
                return true;
            case "WHEEL CHAIR" or "WHEELCHAIR" or "MANUAL WHEELCHAIR":
                mobilityRequirement = cannotTransfer
                    ? PassengerMobilityRequirement.ManualWheelchairCannotTransfer
                    : PassengerMobilityRequirement.ManualWheelchair;
                return true;
            case "MANUAL WHEELCHAIR - CANNOT TRANSFER" or "WHEELCHAIR - CANNOT TRANSFER" or "WHEEL CHAIR - CANNOT TRANSFER":
                mobilityRequirement = PassengerMobilityRequirement.ManualWheelchairCannotTransfer;
                return true;
            case "WHEELCHAIR-ELECTRIC" or "WHEELCHAIR - ELECTRIC" or "ELECTRIC WHEELCHAIR":
                mobilityRequirement = PassengerMobilityRequirement.ElectricWheelchair;
                return true;
            default:
                mobilityRequirement = default;
                return false;
        }
    }
}
