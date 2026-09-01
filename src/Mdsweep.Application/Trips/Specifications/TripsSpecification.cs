using Mdsweep.Application.Common.Models;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Specifications;

public sealed class TripsSpecification : SpecificationBuilder<TripAggregate, Guid, TripsSpecification>
{
    public TripsSpecification WithServiceDate(LocalDate? serviceDate)
    {
        if (!serviceDate.HasValue)
        {
            return this;
        }

        var value = serviceDate.Value.ToDateOnly();

        Spec.Add(query => query.Where(trip => trip.BrokerData.ServiceDate == value));

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

            default:
                throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported trip sorting.");
        }

        Spec.AddSorting(trip => trip.Id);

        return this;
    }
}
