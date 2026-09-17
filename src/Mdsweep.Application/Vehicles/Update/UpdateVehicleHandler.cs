using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Vehicles.Specifications;

namespace Mdsweep.Application.Vehicles.Update;

public sealed class UpdateVehicleHandler(IRepository repository)
{
    public async Task<Result> Handle(UpdateVehicleCommand command, CancellationToken ct)
    {
        var vehicle = await repository.SingleOrDefaultAsync(new VehiclesSpecification().WithId(command.Id).Build(), ct);
        if (vehicle is null)
            return Result.NotFound();
        if (
            await repository.CountAsync(
                new VehiclesSpecification().WithVin(command.Vin).WithoutId(command.Id).Build(),
                ct
            ) > 0
        )
            return Result.Invalid(VehicleErrors.DuplicateVin());

        vehicle.UpdateDetails(command.DisplayLabel, command.Vin);
        return Result.Success();
    }
}
