using Mdsweep.Application.Vehicles.Create;

namespace Mdsweep.Api.Features.Vehicles.Create;

public sealed record CreateVehicleRequest(string DisplayLabel, string Vin)
{
    public CreateVehicleCommand ToCommand() => new(DisplayLabel, Vin);
}
