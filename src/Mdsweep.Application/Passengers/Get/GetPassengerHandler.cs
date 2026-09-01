using Mdsweep.Application.Common.Persistence;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.Get;

public sealed class GetPassengerHandler(IRepository repository)
{
    public async Task<Result<PassengerModel>> Handle(GetPassengerQuery query, CancellationToken ct)
    {
        var passenger = await repository.GetByIdAsync<PassengerAggregate, Guid>(query.Id, ct);

        if (passenger is null)
        {
            return Result.NotFound();
        }

        return Result.Success(PassengerModel.FromAggregate(passenger));
    }
}
