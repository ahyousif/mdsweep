using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Get;

public sealed class GetTripHandler(IRepository repository)
{
    public async Task<Result<TripModel>> Handle(GetTripQuery query, CancellationToken ct)
    {
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(query.Id, ct);

        return trip is null ? Result.NotFound() : Result.Success(TripModel.FromAggregate(trip));
    }
}
