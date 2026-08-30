using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Passengers.Events;

namespace Mdsweep.Domain.Passengers;

public sealed class PassengerAggregate : AggregateRoot<Guid>, ITenanted
{
    private PassengerAggregate()
        : base(default) { }

    private PassengerAggregate(Guid id, string firstName, string lastName)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? TenantId { get; set; }

    public static PassengerAggregate Create(string firstName, string lastName)
    {
        Guard.Against.Null(firstName, nameof(firstName));
        Guard.Against.Null(lastName, nameof(lastName));

        var passenger = new PassengerAggregate(Guid.CreateVersion7(), firstName, lastName);

        passenger.AddDomainEvent(new PassengerCreatedDomainEvent(passenger.Id));

        return passenger;
    }
}
