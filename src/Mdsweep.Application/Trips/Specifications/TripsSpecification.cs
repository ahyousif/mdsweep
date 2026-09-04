using Mdsweep.Application.Common.Models;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Specifications;

public sealed class TripsSpecification : SpecificationBuilder<TripAggregate, Guid, TripsSpecification>
{
    public TripsSpecification WithTripDateRange(LocalDate? startDate, LocalDate? endDate)
    {
        if (!startDate.HasValue && !endDate.HasValue)
        {
            return this;
        }

        if (startDate.HasValue)
        {
            var start = startDate.Value.ToDateOnly();
            Spec.Add(query => query.Where(trip => trip.BrokerData.ServiceDate >= start));
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.ToDateOnly();
            Spec.Add(query => query.Where(trip => trip.BrokerData.ServiceDate <= end));
        }

        return this;
    }

    public TripsSpecification WithSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return this;

        var value = search.Trim().ToUpperInvariant();
        Spec.Add(query => query.Where(trip =>
            trip.BrokerTripNumber.Contains(value) ||
            trip.Passenger.FirstName.ToUpper().Contains(value) ||
            trip.Passenger.LastName.ToUpper().Contains(value)));
        return this;
    }

    public TripsSpecification WithNeedsAttention(bool? needsAttention)
    {
        if (!needsAttention.HasValue) return this;

        Spec.Add(query => query.Where(trip =>
            ((trip.ScheduledPickupTime == null && !trip.BrokerData.IsWillCall) ||
            (trip.BrokerData.BrokerStatus != null && trip.BrokerData.BrokerStatus != "VALID") ||
            trip.BrokerData.MobilityRequirement == PassengerMobilityRequirement.Unknown) == needsAttention.Value));
        return this;
    }

    public TripsSpecification WithBrokerStatus(string? brokerStatus)
    {
        if (brokerStatus is null)
        {
            return this;
        }

        Spec.Add(query => query.Where(trip => trip.BrokerData.BrokerStatus == brokerStatus));

        return this;
    }

    public TripsSpecification WithWillCall(bool? isWillCall)
    {
        if (!isWillCall.HasValue)
        {
            return this;
        }

        var value = isWillCall.Value;

        Spec.Add(query => query.Where(trip => trip.BrokerData.IsWillCall == value));

        return this;
    }

    public TripsSpecification OrderBy(TripSortBy sortBy, SortDirection direction)
    {
        var descending = direction switch
        {
            SortDirection.Ascending => false,
            SortDirection.Descending => true,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported sort direction."),
        };

        switch (sortBy)
        {
            case TripSortBy.AppointmentTime:
                Spec.AddSorting(trip => trip.BrokerData.AppointmentTime, descending);

                Spec.AddSorting(trip => trip.BrokerData.ServiceDate, descending);
                break;

            case TripSortBy.ServiceDate:
                Spec.AddSorting(trip => trip.BrokerData.ServiceDate, descending);
                break;

            case TripSortBy.BrokerTripNumber:
                Spec.AddSorting(trip => trip.BrokerTripNumber, descending);
                break;

            case TripSortBy.ScheduledPickupTime:
                Spec.AddSorting(trip => trip.ScheduledPickupTime, descending);
                break;

            case TripSortBy.PassengerName:
                Spec.AddSorting(trip => trip.Passenger.LastName, descending);
                Spec.AddSorting(trip => trip.Passenger.FirstName, descending);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported trip sorting.");
        }

        Spec.AddSorting(trip => trip.Id);

        return this;
    }
}
