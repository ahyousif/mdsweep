using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Trips;
using JasperFx.MultiTenancy;
using Wolverine.Attributes;

namespace Mdsweep.Application.Trips.Get;

public static class GetTripHandler
{
    [Transactional(typeof(IRepository))]
    public static async Task<Result<TripModel>> Handle(
        GetTripQuery query,
        IRepository repository,
        TenantId activeTenant,
        CancellationToken ct
    )
    {
        _ = activeTenant;
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(query.Id, ct);
        return trip is null ? Result.NotFound() : Result.Success(TripModel.FromAggregate(trip));
    }
}
