using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Vehicles.Specifications;

namespace Mdsweep.Application.Vehicles.List;

public sealed class ListVehiclesHandler(IRepository repository)
{
    public async Task<Result<IReadOnlyList<VehicleModel>>> Handle(ListVehiclesQuery query, CancellationToken ct)
    {
        var vehicles = await repository.ListAsync(
            new VehiclesSpecification().OrderBy(x => x.DisplayLabel).AsNoTracking().Build(),
            ct
        );
        return Result.Success<IReadOnlyList<VehicleModel>>(vehicles.Select(VehicleModel.FromAggregate).ToList());
    }
}
