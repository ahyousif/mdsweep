using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers;

public sealed record PassengerModel
{
    public Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? BrokerMemberId { get; init; }

    public static PassengerModel FromAggregate(PassengerAggregate passenger) =>
        new()
        {
            Id = passenger.Id,
            FirstName = passenger.FirstName,
            LastName = passenger.LastName,
            BrokerMemberId = passenger.BrokerMemberId,
        };
}
