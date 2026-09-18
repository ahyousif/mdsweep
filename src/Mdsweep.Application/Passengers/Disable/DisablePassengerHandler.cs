using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.Disable;

public sealed class DisablePassengerHandler(IRepository repository)
{
    public async Task<Result> Handle(DisablePassengerCommand command, CancellationToken ct)
    {
        var passenger = await repository.GetByIdAsync<PassengerAggregate, Guid>(command.PassengerId, ct);

        if (passenger is null)
        {
            return Result.NotFound();
        }

        passenger.Disable();

        return Result.Success();
    }
}
