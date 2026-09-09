using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Passengers;
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
    public PassengerAggregate Passenger { get; private set; } = null!;
    public string BrokerTripNumber { get; private set; } = null!;
    public BrokerTripData BrokerData { get; private set; } = null!;
    public LocalTime? CalculatedPickupTime { get; private set; }
    public LocalTime? ManualPickupTime { get; private set; }
    public int? EstimatedTravelMinutes { get; private set; }
    public int? EstimatedDistanceMeters { get; private set; }
    public string? ScheduleInputFingerprint { get; private set; }
    public LocalTime? ScheduledPickupTime => ManualPickupTime ?? CalculatedPickupTime;

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

    public void OverridePickupTime(LocalTime pickupTime)
    {
        ManualPickupTime = pickupTime;
    }

    public void RemovePickupOverride()
    {
        ManualPickupTime = null;
    }

    public void UpdateBrokerData(BrokerTripData brokerData)
    {
        Guard.Against.Null(brokerData);

        BrokerData = brokerData;
    }

    public void UpdateCalculatedSchedule(
        LocalTime? pickupTime,
        string? scheduleInputFingerprint,
        int? estimatedTravelMinutes = null,
        int? estimatedDistanceMeters = null
    )
    {
        CalculatedPickupTime = pickupTime;
        ScheduleInputFingerprint = scheduleInputFingerprint;
        EstimatedTravelMinutes = estimatedTravelMinutes;
        EstimatedDistanceMeters = estimatedDistanceMeters;
    }
}
