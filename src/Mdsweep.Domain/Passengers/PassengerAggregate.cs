using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Passengers.Events;

namespace Mdsweep.Domain.Passengers;

public sealed class PassengerAggregate : AggregateRoot<Guid>, ITenanted
{
    private PassengerAggregate()
        : base(default) { }

    private PassengerAggregate(Guid id, string? brokerMemberId, string firstName, string lastName)
        : base(id)
    {
        BrokerMemberId = brokerMemberId;
        FirstName = firstName;
        LastName = lastName;
    }

    public string? TenantId { get; set; }

    public string? BrokerMemberId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public static PassengerAggregate Create(string? brokerMemberId, string firstName, string lastName)
    {
        Guard.Against.NullOrWhiteSpace(brokerMemberId, nameof(brokerMemberId));

        var passenger = new PassengerAggregate(
            Guid.CreateVersion7(),
            brokerMemberId!.ToUpperInvariant(),
            Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName)),
            Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName))
        );

        passenger.AddDomainEvent(new PassengerCreatedDomainEvent(passenger.Id));

        return passenger;
    }

    public void UpdateDetails(string firstName, string lastName)
    {
        FirstName = Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
        LastName = Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));
    }
}
