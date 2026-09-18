using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Passengers.Specifications;

namespace Mdsweep.Application.Passengers.Get;

public sealed class GetPassengerHandler(IRepository repository)
{
    public async Task<Result<PassengerModel>> Handle(GetPassengerQuery query, CancellationToken ct)
    {
        var passenger = await repository.SingleOrDefaultAsync(
            new PassengersSpecification().WithId(query.Id).AsNoTracking().Build(PassengerModelProjection.Instance),
            ct
        );

        if (passenger is null)
        {
            return Result.NotFound();
        }

        return Result.Success(passenger);
    }
}
