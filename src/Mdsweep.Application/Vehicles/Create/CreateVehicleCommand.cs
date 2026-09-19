using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Vehicles.Create;

public sealed record CreateVehicleCommand(
    string DisplayLabel,
    string Vin,
    int? Year = null,
    string? Make = null,
    string? Model = null
) : ICommand<VehicleModel>;
