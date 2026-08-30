using JasperFx.MultiTenancy;
using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.Create;

public sealed class CreatePassengerHandler(IRepository<PassengerAggregate, Guid> repository)
{
    public async Task<Guid> Handle(CreatePassengerCommand command, CancellationToken ct)
    {
        var passenger = PassengerAggregate.Create(command.FirstName, command.LastName);

        await repository.AddAsync(passenger, ct);

        return passenger.Id;
    }
}
