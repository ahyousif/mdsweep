using Mdsweep.Application.Common.Models;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Specifications;

public sealed class TripsSpecification : SpecificationBuilder<TripAggregate, Guid, TripsSpecification>
{
    public TripsSpecification WithBrokerTripNumbers(IReadOnlyCollection<string> tripNumbers)
    {
        if (tripNumbers.Count == 0)
        {
            return this;
        }

        Spec.Add(query => query.Where(trip => tripNumbers.Contains(trip.BrokerTripNumber)));

        return this;
    }

    public TripsSpecification OrderBy(TripSortBy sortBy, SortDirection direction, bool groupByDate = false)
    {
        var descending = direction switch
        {
            SortDirection.Ascending => false,
            SortDirection.Descending => true,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported sort direction."),
        };

        if (groupByDate && sortBy != TripSortBy.ServiceDate)
        {
            Spec.AddSorting(trip => trip.BrokerData.ServiceDate);
        }

        switch (sortBy)
        {
            case TripSortBy.AppointmentTime:
                Spec.AddSorting(trip => trip.BrokerData.ServiceDate, descending);
                break;

            case TripSortBy.ServiceDate:
                Spec.AddSorting(trip => trip.BrokerData.ServiceDate, descending);
                break;

            case TripSortBy.BrokerTripNumber:
                Spec.AddSorting(trip => trip.BrokerTripNumber, descending);
                break;

            case TripSortBy.ScheduledPickupTime:
                Spec.AddSorting(trip => trip.BrokerData.ServiceDate, descending);
                break;

            case TripSortBy.PassengerName:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported trip sorting.");
        }

        Spec.AddSorting(trip => trip.Id);

        return this;
    }
}
