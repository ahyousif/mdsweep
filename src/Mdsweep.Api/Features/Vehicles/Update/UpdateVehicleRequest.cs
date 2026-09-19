using Mdsweep.Application.Vehicles.Update;

namespace Mdsweep.Api.Features.Vehicles.Update;

public sealed record UpdateVehicleRequest(
    string DisplayLabel,
    string Vin,
    int? Year = null,
    string? Make = null,
    string? Model = null
)
{
    public UpdateVehicleCommand ToCommand(Guid id) => new(id, DisplayLabel, Vin, Year, Make, Model);
}
