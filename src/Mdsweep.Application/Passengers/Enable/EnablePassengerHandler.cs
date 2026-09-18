using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.Enable;

public sealed class EnablePassengerHandler(IRepository repository)
{
    public async Task<Result> Handle(EnablePassengerCommand command, CancellationToken ct)
    {
        var passenger = await repository.GetByIdAsync<PassengerAggregate, Guid>(command.PassengerId, ct);
        if (passenger is null) return Result.NotFound();

        passenger.Enable();
        return Result.Success();
    }
}
