using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Vehicles.Specifications;
using Mdsweep.Domain.Vehicles;

namespace Mdsweep.Application.Vehicles.Create;

public sealed class CreateVehicleHandler(IRepository repository)
{
    public async Task<Result<VehicleModel>> Handle(CreateVehicleCommand command, CancellationToken ct)
    {
        if (await repository.CountAsync(new VehiclesSpecification().WithVin(command.Vin).Build(), ct) > 0)
        {
            return Result.Invalid(VehicleErrors.DuplicateVin());
        }

        var vehicle = VehicleAggregate.Create(command.DisplayLabel, command.Vin);
        await repository.AddAsync(vehicle, ct);
        return Result.Success(VehicleModel.FromAggregate(vehicle));
    }
}
