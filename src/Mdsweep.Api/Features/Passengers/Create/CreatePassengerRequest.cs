using Mdsweep.Application.Passengers.Create;

namespace Mdsweep.Api.Features.Passengers.Create;

public sealed record CreatePassengerRequest(string? BrokerMemberId, string FirstName, string LastName)
{
    public CreatePassengerCommand ToCommand() => new(BrokerMemberId, FirstName, LastName);
}
