using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
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

    // Stamped and filtered by Wolverine's conjoined-tenancy integration.
    public string? TenantId { get; set; }
    public string? BrokerMemberId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public static PassengerAggregate Create(string? brokerMemberId, string firstName, string lastName)
    {
        Guard.Against.Invalid(
            brokerMemberId is not null && string.IsNullOrWhiteSpace(brokerMemberId),
            "Broker member ID cannot be blank when supplied."
        );
        Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
        Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));

        var passenger = new PassengerAggregate(Guid.CreateVersion7(), brokerMemberId?.ToUpperInvariant(), firstName, lastName);

        passenger.AddDomainEvent(new PassengerCreatedDomainEvent(passenger.Id));

        return passenger;
    }
}
