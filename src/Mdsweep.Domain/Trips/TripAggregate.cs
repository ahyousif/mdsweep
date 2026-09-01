using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Trips.Events;

namespace Mdsweep.Domain.Trips;

public sealed class TripAggregate : AggregateRoot<Guid>, ITenanted
{
    private TripAggregate()
        : base(default) { }

    private TripAggregate(Guid id, Guid passengerId, string brokerTripNumber, BrokerTripData brokerData)
        : base(id)
    {
        PassengerId = passengerId;
        BrokerTripNumber = brokerTripNumber;
        BrokerData = brokerData;
    }

    public string? TenantId { get; set; }
    public Guid PassengerId { get; private set; }
    public string BrokerTripNumber { get; private set; } = null!;
    public BrokerTripData BrokerData { get; private set; } = null!;
    public LocalTime? ScheduledPickupTime { get; private set; }

    public static TripAggregate Create(Guid passengerId, string brokerTripNumber, BrokerTripData brokerData)
    {
        Guard.Against.Default(passengerId, nameof(passengerId));
        Guard.Against.NullOrWhiteSpace(brokerTripNumber, nameof(brokerTripNumber));
        Guard.Against.Null(brokerData, nameof(brokerData));

        var trip = new TripAggregate(Guid.CreateVersion7(), passengerId, brokerTripNumber, brokerData);

        trip.AddDomainEvent(new TripCreatedDomainEvent(trip.Id, trip.PassengerId, trip.BrokerTripNumber));

        return trip;
    }

    public void ReconcileBrokerData(BrokerTripData brokerData)
    {
        Guard.Against.Null(brokerData, nameof(brokerData));

        if (BrokerData == brokerData)
        {
            return;
        }

        BrokerData = brokerData;

        AddDomainEvent(new TripBrokerDataReconciledDomainEvent(Id, BrokerTripNumber));
    }

    public void SetScheduledPickupTime(LocalTime scheduledPickupTime)
    {
        ScheduledPickupTime = scheduledPickupTime;
    }
}
