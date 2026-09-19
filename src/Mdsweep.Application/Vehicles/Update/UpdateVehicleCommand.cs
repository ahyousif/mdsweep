using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Vehicles.Update;

public sealed record UpdateVehicleCommand(
    Guid Id,
    string DisplayLabel,
    string Vin,
    int? Year = null,
    string? Make = null,
    string? Model = null
) : ICommand;
