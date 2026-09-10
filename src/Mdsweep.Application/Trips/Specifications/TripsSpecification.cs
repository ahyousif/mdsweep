using Mdsweep.Application.Common.Models;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Specifications;

public sealed class TripsSpecification : SpecificationBuilder<TripAggregate, Guid, TripsSpecification>
{
    public TripsSpecification WithDateRange(LocalDate? startDate, LocalDate? endDate)
    {
        if (startDate.HasValue)
        {
            Spec.Add(query => query.Where(trip => trip.BrokerData.ServiceDate >= startDate.Value));
        }

        if (endDate.HasValue)
        {
            Spec.Add(query => query.Where(trip => trip.BrokerData.ServiceDate <= endDate.Value));
        }

        return this;
    }

#pragma warning disable CA1862 // EF Core does not translate StringComparison overloads.
    public TripsSpecification WithSearch(string? query)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            var value = query.Trim().ToUpperInvariant();

            Spec.Add(q =>
                q.Where(trip =>
                    trip.BrokerTripNumber.ToUpper().Contains(value)
                    || trip.Passenger.FirstName.ToUpper().Contains(value)
                    || trip.Passenger.LastName.ToUpper().Contains(value)
                    || (
                        trip.Passenger.BrokerMemberId != null && trip.Passenger.BrokerMemberId.ToUpper().Contains(value)
                    )
                    || trip.BrokerData.PickupAddress.ToUpper().Contains(value)
                    || trip.BrokerData.PickupCity.ToUpper().Contains(value)
                    || trip.BrokerData.DropoffAddress.ToUpper().Contains(value)
                    || trip.BrokerData.DropoffCity.ToUpper().Contains(value)
                )
            );
        }
        return this;
    }
#pragma warning restore CA1862

    public TripsSpecification WithBrokerStatus(string? brokerStatus)
    {
        if (!string.IsNullOrWhiteSpace(brokerStatus))
        {
            Spec.Add(query => query.Where(trip => trip.BrokerData.Status == brokerStatus));
        }

        return this;
    }

    public TripsSpecification WithTripNumbers(IReadOnlyCollection<string> tripNumbers)
    {
        if (tripNumbers.Count > 0)
        {
            Spec.Add(query => query.Where(trip => tripNumbers.Contains(trip.BrokerTripNumber)));
        }

        return this;
    }

    public TripsSpecification WithWillCall(bool? willCall)
    {
        if (willCall.HasValue)
        {
            Spec.Add(query => query.Where(trip => trip.BrokerData.IsWillCall == willCall));
        }

        return this;
    }

    // sorting
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
                Spec.AddSorting(trip => trip.BrokerData.AppointmentTime == null);

                Spec.AddSorting(trip => trip.BrokerData.AppointmentTime, descending);

                break;

            case TripSortBy.ServiceDate:
                Spec.AddSorting(trip => trip.BrokerData.ServiceDate, descending);

                break;

            case TripSortBy.BrokerTripNumber:
                Spec.AddSorting(trip => trip.BrokerTripNumber, descending);

                break;

            case TripSortBy.ScheduledPickupTime:
                Spec.AddSorting(trip =>
                    trip.ManualPickupTime == null
                    && trip.CalculatedPickupTime == null
                    && trip.BrokerData.BrokerPickupTime == null
                );

                Spec.AddSorting(
                    trip => trip.ManualPickupTime ?? trip.CalculatedPickupTime ?? trip.BrokerData.BrokerPickupTime,
                    descending
                );

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
