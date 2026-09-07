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
    public string? BrokerDataFingerPrint { get; set; } = null!;
    public LocalTime? CalculatedPickupTime { get; private set; }
    public LocalTime? ManualPickupTime { get; private set; }
    public string? ScheduleInputFingerprint { get; private set; }
    public LocalTime? ScheduledPickupTime => CalculatedPickupTime ?? ManualPickupTime;

    public static TripAggregate Create(Guid passengerId, string brokerTripNumber, BrokerTripData brokerData)
    {
        Guard.Against.Default(passengerId, nameof(passengerId));
        Guard.Against.NullOrWhiteSpace(brokerTripNumber, nameof(brokerTripNumber));
        Guard.Against.Null(brokerData, nameof(brokerData));

        var trip = new TripAggregate(
            Guid.CreateVersion7(),
            passengerId,
            brokerTripNumber.ToUpperInvariant(),
            brokerData
        );

        trip.AddDomainEvent(new TripCreatedDomainEvent(trip.Id, trip.PassengerId, trip.BrokerTripNumber));

        return trip;
    }
}
