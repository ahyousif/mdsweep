using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips.Events;

namespace Mdsweep.Domain.Trips;

public sealed class TripAggregate : AggregateRoot<Guid>, ITenanted
{
    private TripAggregate()
        : base(default) { }

    private TripAggregate(Guid id, Guid journeyId, Guid passengerId, string brokerTripNumber, BrokerTripData brokerData)
        : base(id)
    {
        JourneyId = journeyId;
        PassengerId = passengerId;
        BrokerTripNumber = brokerTripNumber;
        BrokerData = brokerData;
    }

    public string? TenantId { get; set; }
    public Guid JourneyId { get; private set; }
    public Guid PassengerId { get; private set; }
    public PassengerAggregate Passenger { get; private set; } = null!;
    public string BrokerTripNumber { get; private set; } = null!;
    public BrokerTripData BrokerData { get; private set; } = null!;
    public LocalTime? CalculatedPickupTime { get; private set; }
    public LocalTime? ManualPickupTime { get; private set; }
    public int? EstimatedTravelMinutes { get; private set; }
    public int? EstimatedDistanceMeters { get; private set; }

    public LocalTime? ScheduledPickupTime => ManualPickupTime ?? CalculatedPickupTime ?? BrokerData.BrokerPickupTime;

    public bool RequiresRouteEstimate => BrokerData.AppointmentTime is not null;

    public static TripAggregate Create(
        Guid journeyId,
        Guid passengerId,
        string brokerTripNumber,
        BrokerTripData brokerData
    )
    {
        Guard.Against.Default(journeyId, nameof(journeyId));
        Guard.Against.Default(passengerId, nameof(passengerId));
        Guard.Against.NullOrWhiteSpace(brokerTripNumber, nameof(brokerTripNumber));
        Guard.Against.Null(brokerData, nameof(brokerData));

        var trip = new TripAggregate(
            Guid.CreateVersion7(),
            journeyId,
            passengerId,
            brokerTripNumber.ToUpperInvariant(),
            brokerData
        );

        trip.AddDomainEvent(new TripCreatedDomainEvent(trip.Id, trip.PassengerId, trip.BrokerTripNumber));

        return trip;
    }

    public void ChangeJourney(JourneyAggregate source, JourneyAggregate destination)
    {
        Guard.Against.Null(source);
        Guard.Against.Null(destination);
        Guard.Against.Invalid(source.Id != JourneyId, "The source Journey must contain this Trip.");
        Guard.Against.Invalid(source.Id == destination.Id, "The destination Journey must differ from the source.");

        source.MarkManual();
        destination.MarkManual();
        JourneyId = destination.Id;
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
        ClearRouteEstimate();
    }

    public void ApplyRouteEstimate(Duration duration, int distanceMeters, int pickupBufferMinutes)
    {
        Guard.Against.Invalid(
            BrokerData.AppointmentTime is null,
            "A trip requires an appointment time before a route estimate can be applied."
        );

        var travelMinutes = (int)Math.Ceiling(duration.TotalMinutes);

        CalculatedPickupTime = BrokerData.AppointmentTime!.Value.PlusMinutes(-(travelMinutes + pickupBufferMinutes));

        EstimatedTravelMinutes = travelMinutes;
        EstimatedDistanceMeters = distanceMeters;
    }

    public void ClearRouteEstimate()
    {
        CalculatedPickupTime = null;
        EstimatedTravelMinutes = null;
        EstimatedDistanceMeters = null;
    }
}
