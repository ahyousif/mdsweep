using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Vehicles.Specifications;

namespace Mdsweep.Application.Vehicles.Get;

public sealed class GetVehicleHandler(IRepository repository)
{
    public async Task<Result<VehicleModel>> Handle(GetVehicleQuery query, CancellationToken ct)
    {
        var vehicle = await repository.SingleOrDefaultAsync(
            new VehiclesSpecification().WithId(query.Id).AsNoTracking().Build(),
            ct
        );
        return vehicle is null ? Result.NotFound() : Result.Success(VehicleModel.FromAggregate(vehicle));
    }
}
