using Mdsweep.Application.Common.Models;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.List;

public sealed class ListTripsSpecification : Specification<TripAggregate, TripModel>
{
    public ListTripsSpecification(ListTripsQuery query)
    {
        var serviceDate = query.ServiceDate?.ToDateOnly();

        Query
            .Where(trip => trip.BrokerData.ServiceDate == serviceDate, serviceDate.HasValue)
            .Where(trip => trip.BrokerData.BrokerStatus == query.BrokerStatus, query.BrokerStatus is not null)
            .Where(trip => trip.BrokerData.IsWillCall == query.IsWillCall, query.IsWillCall.HasValue);

        switch (query.SortBy, query.SortDirection)
        {
            case (TripSortBy.AppointmentTime, SortDirection.Asc):
                Query.OrderBy(trip => trip.BrokerData.AppointmentTime).ThenBy(trip => trip.Id);
                break;
            case (TripSortBy.AppointmentTime, SortDirection.Desc):
                Query.OrderByDescending(trip => trip.BrokerData.AppointmentTime).ThenBy(trip => trip.Id);
                break;
            case (TripSortBy.ServiceDate, SortDirection.Asc):
                Query.OrderBy(trip => trip.BrokerData.ServiceDate).ThenBy(trip => trip.Id);
                break;
            case (TripSortBy.ServiceDate, SortDirection.Desc):
                Query.OrderByDescending(trip => trip.BrokerData.ServiceDate).ThenBy(trip => trip.Id);
                break;
            case (TripSortBy.BrokerTripNumber, SortDirection.Asc):
                Query.OrderBy(trip => trip.BrokerTripNumber).ThenBy(trip => trip.Id);
                break;
            case (TripSortBy.BrokerTripNumber, SortDirection.Desc):
                Query.OrderByDescending(trip => trip.BrokerTripNumber).ThenBy(trip => trip.Id);
                break;
            case (TripSortBy.ScheduledPickupTime, SortDirection.Asc):
                Query.OrderBy(trip => trip.ScheduledPickupTime).ThenBy(trip => trip.Id);
                break;
            case (TripSortBy.ScheduledPickupTime, SortDirection.Desc):
                Query.OrderByDescending(trip => trip.ScheduledPickupTime).ThenBy(trip => trip.Id);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(query.SortBy));
        }

        Query
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(trip => new TripModel(
                trip.Id,
                trip.BrokerTripNumber,
                LocalDate.FromDateOnly(trip.BrokerData.ServiceDate),
                trip.BrokerData.AppointmentTime,
                trip.BrokerData.BrokerStatus,
                trip.BrokerData.IsWillCall,
                trip.ScheduledPickupTime
            ));
    }
}

internal sealed class CountTripsSpecification : Specification<TripAggregate>
{
    public CountTripsSpecification(ListTripsQuery query)
    {
        var serviceDate = query.ServiceDate?.ToDateOnly();

        Query
            .Where(trip => trip.BrokerData.ServiceDate == serviceDate, serviceDate.HasValue)
            .Where(trip => trip.BrokerData.BrokerStatus == query.BrokerStatus, query.BrokerStatus is not null)
            .Where(trip => trip.BrokerData.IsWillCall == query.IsWillCall, query.IsWillCall.HasValue);
    }
}
