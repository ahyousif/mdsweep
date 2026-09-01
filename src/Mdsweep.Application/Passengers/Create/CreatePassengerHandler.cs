using Mdsweep.Application.Common.Persistence;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.Create;

public sealed class CreatePassengerHandler(IRepository repository)
{
    public async Task<Result<Guid>> Handle(CreatePassengerCommand command, CancellationToken ct)
    {
        var validationErrors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            validationErrors.Add(
                new ValidationError { Identifier = nameof(command.FirstName), ErrorMessage = "First name is required." }
            );
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            validationErrors.Add(
                new ValidationError { Identifier = nameof(command.LastName), ErrorMessage = "Last name is required." }
            );
        }

        if (validationErrors.Count > 0)
        {
            return Result.Invalid(validationErrors);
        }

        var passenger = PassengerAggregate.Create(
            command.BrokerMemberId,
            command.FirstName,
            command.LastName
        );

        await repository.AddAsync(passenger, ct);

        return Result.Success(passenger.Id);
    }
}
