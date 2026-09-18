using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.Update;

public sealed class UpdatePassengerHandler(IRepository repository)
{
    public async Task<Result> Handle(UpdatePassengerCommand command, CancellationToken ct)
    {
        var passenger = await repository.GetByIdAsync<PassengerAggregate, Guid>(command.PassengerId, ct);

        if (passenger is null)
        {
            return Result.NotFound();
        }

        passenger.UpdateDetails(
            command.BrokerMemberId,
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            command.PhoneNumber,
            command.AlternatePhoneNumber,
            command.PassengerType,
            command.SpecialNeeds,
            command.Notes
        );

        return Result.Success();
    }
}
