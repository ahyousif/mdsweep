using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Trips.Specifications;

namespace Mdsweep.Application.Trips.Get;

public sealed class GetTripHandler(IRepository repository)
{
    public async Task<Result<TripModel>> Handle(GetTripQuery query, CancellationToken ct)
    {
        var specification = new TripsSpecification().WithId(query.Id).Build(TripModelProjection.Instance);

        var trip = await repository.SingleOrDefaultAsync(specification, ct);

        return trip is null ? Result.NotFound() : Result.Success(trip);
    }
}
