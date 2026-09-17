using Mdsweep.Application.Vehicles.SetActive;

namespace Mdsweep.Api.Features.Vehicles.SetActive;

public sealed record SetVehicleActiveRequest(bool? IsActive)
{
    public SetVehicleActiveCommand ToCommand(Guid id) => new(id, IsActive!.Value);
}
