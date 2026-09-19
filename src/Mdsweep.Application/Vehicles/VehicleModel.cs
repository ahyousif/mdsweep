using Mdsweep.Domain.Vehicles;

namespace Mdsweep.Application.Vehicles;

public sealed record VehicleModel(
    Guid Id,
    string DisplayLabel,
    string Vin,
    bool IsActive,
    int? Year = null,
    string? Make = null,
    string? Model = null
)
{
    public static VehicleModel FromAggregate(VehicleAggregate vehicle) =>
        new(vehicle.Id, vehicle.DisplayLabel, vehicle.Vin, vehicle.IsActive, vehicle.Year, vehicle.Make, vehicle.Model);
}
