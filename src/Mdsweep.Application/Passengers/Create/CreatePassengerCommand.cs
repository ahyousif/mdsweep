using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Passengers.Create;

public record CreatePassengerCommand(string? BrokerMemberId, string FirstName, string LastName)
    : ICommand<Guid>;
