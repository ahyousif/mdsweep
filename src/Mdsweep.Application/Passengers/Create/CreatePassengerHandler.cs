using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.Create;

public sealed class CreatePassengerHandler(IRepository repository)
{
    public async Task<Result<Guid>> Handle(CreatePassengerCommand command, CancellationToken ct)
    {
        var passenger = PassengerAggregate.Create(command.BrokerMemberId, command.FirstName, command.LastName);

        await repository.AddAsync(passenger, ct);

        return Result.Success(passenger.Id);
    }
}
