using Mdsweep.Application.Vehicles.Update;

namespace Mdsweep.Api.Features.Vehicles.Update;

public sealed record UpdateVehicleRequest(string DisplayLabel, string Vin)
{
    public UpdateVehicleCommand ToCommand(Guid id) => new(id, DisplayLabel, Vin);
}
