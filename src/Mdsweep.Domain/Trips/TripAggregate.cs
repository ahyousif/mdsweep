using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Trips.Events;

namespace Mdsweep.Domain.Trips;

public sealed class TripAggregate : AggregateRoot<Guid>, ITenanted
{
    private TripAggregate()
        : base(default) { }

    private TripAggregate(Guid id, Guid passengerId, string brokerTripNumber, BrokerTripFacts brokerFacts)
        : base(id)
    {
        PassengerId = passengerId;
        BrokerTripNumber = brokerTripNumber;
        BrokerFacts = brokerFacts;
    }

    // Stamped and filtered by Wolverine's conjoined-tenancy integration.
    public string? TenantId { get; set; }
    public Guid PassengerId { get; private set; }
    public string BrokerTripNumber { get; private set; } = null!;
    public BrokerTripFacts BrokerFacts { get; private set; } = null!;
    public LocalTime? ScheduledPickupTime { get; private set; }

    public static TripAggregate Create(Guid passengerId, string brokerTripNumber, BrokerTripFacts brokerFacts)
    {
        Guard.Against.Default(passengerId, nameof(passengerId));
        Guard.Against.NullOrWhiteSpace(brokerTripNumber, nameof(brokerTripNumber));
        Guard.Against.Null(brokerFacts, nameof(brokerFacts));

        var trip = new TripAggregate(Guid.CreateVersion7(), passengerId, brokerTripNumber, brokerFacts);
        trip.AddDomainEvent(new TripCreatedDomainEvent(trip.Id, trip.PassengerId, trip.BrokerTripNumber));
        return trip;
    }

    public void ReconcileBrokerFacts(BrokerTripFacts brokerFacts)
    {
        Guard.Against.Null(brokerFacts, nameof(brokerFacts));
        if (BrokerFacts == brokerFacts)
        {
            return;
        }
        BrokerFacts = brokerFacts;
        AddDomainEvent(new TripBrokerFactsReconciledDomainEvent(Id, BrokerTripNumber));
    }

    public void SetScheduledPickupTime(LocalTime scheduledPickupTime)
    {
        ScheduledPickupTime = scheduledPickupTime;
    }
}

public sealed record BrokerTripFacts(
    DateOnly ServiceDate,
    LocalTime? AppointmentTime,
    string PickupAddress,
    string PickupCity,
    string DropoffAddress,
    string DropoffCity,
    string? BrokerStatus,
    bool IsWillCall
);
