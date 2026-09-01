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
        Spec.Add(query =>
        {
            switch (sortBy, direction)
            {
                case (TripSortBy.AppointmentTime, SortDirection.Ascending):
                    query
                        .OrderBy(trip => trip.BrokerData.AppointmentTime)
                        .ThenBy(trip => trip.BrokerData.ServiceDate)
                        .ThenBy(trip => trip.Id);
                    break;

                case (TripSortBy.AppointmentTime, SortDirection.Descending):
                    query
                        .OrderByDescending(trip => trip.BrokerData.AppointmentTime)
                        .ThenByDescending(trip => trip.BrokerData.ServiceDate)
                        .ThenByDescending(trip => trip.Id);
                    break;

                case (TripSortBy.ServiceDate, SortDirection.Ascending):
                    query.OrderBy(trip => trip.BrokerData.ServiceDate).ThenBy(trip => trip.Id);
                    break;

                case (TripSortBy.ServiceDate, SortDirection.Descending):
                    query.OrderByDescending(trip => trip.BrokerData.ServiceDate).ThenBy(trip => trip.Id);
                    break;

                case (TripSortBy.BrokerTripNumber, SortDirection.Ascending):
                    query.OrderBy(trip => trip.BrokerTripNumber).ThenBy(trip => trip.Id);
                    break;

                case (TripSortBy.BrokerTripNumber, SortDirection.Descending):
                    query.OrderByDescending(trip => trip.BrokerTripNumber).ThenBy(trip => trip.Id);
                    break;

                case (TripSortBy.ScheduledPickupTime, SortDirection.Ascending):
                    query.OrderBy(trip => trip.ScheduledPickupTime).ThenBy(trip => trip.Id);
                    break;

                case (TripSortBy.ScheduledPickupTime, SortDirection.Descending):
                    query.OrderByDescending(trip => trip.ScheduledPickupTime).ThenBy(trip => trip.Id);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported trip sorting.");
            }
        });

        return this;
    }
}
