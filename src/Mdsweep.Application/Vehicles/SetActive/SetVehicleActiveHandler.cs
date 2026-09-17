using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Vehicles.Specifications;

namespace Mdsweep.Application.Vehicles.SetActive;

public sealed class SetVehicleActiveHandler(IRepository repository)
{
    public async Task<Result> Handle(SetVehicleActiveCommand command, CancellationToken ct)
    {
        var vehicle = await repository.SingleOrDefaultAsync(new VehiclesSpecification().WithId(command.Id).Build(), ct);
        if (vehicle is null)
            return Result.NotFound();
        vehicle.SetActive(command.IsActive);
        return Result.Success();
    }
}
