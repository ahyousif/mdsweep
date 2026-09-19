using Mdsweep.Application.Vehicles.Create;

namespace Mdsweep.Api.Features.Vehicles.Create;

public sealed record CreateVehicleRequest(
    string DisplayLabel,
    string Vin,
    int? Year = null,
    string? Make = null,
    string? Model = null
)
{
    public CreateVehicleCommand ToCommand() => new(DisplayLabel, Vin, Year, Make, Model);
}
