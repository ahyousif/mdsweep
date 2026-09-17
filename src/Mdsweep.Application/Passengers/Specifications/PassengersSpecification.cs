using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.Specifications;

public sealed class PassengersSpecification : SpecificationBuilder<PassengerAggregate, Guid, PassengersSpecification>
{
    public PassengersSpecification WithBrokerMemberIds(IReadOnlyCollection<string> memberIds)
    {
        if (memberIds.Count == 0)
        {
            return this;
        }

        Spec.Add(query => query.Where(passenger => memberIds.Contains(passenger.BrokerMemberId)));

        return this;
    }

#pragma warning disable CA1862 // EF Core does not translate StringComparison overloads.
    public PassengersSpecification WithSearch(string? search)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToUpperInvariant();

            Spec.Add(query =>
                query.Where(passenger =>
                    passenger.FirstName.ToUpper().Contains(value)
                    || passenger.LastName.ToUpper().Contains(value)
                    || (passenger.BrokerMemberId != null && passenger.BrokerMemberId.ToUpper().Contains(value))
                    || (passenger.PhoneNumber != null && passenger.PhoneNumber.ToUpper().Contains(value))
                    || (
                        passenger.AlternatePhoneNumber != null
                        && passenger.AlternatePhoneNumber.ToUpper().Contains(value)
                    )
                )
            );
        }

        return this;
    }
#pragma warning restore CA1862

    public PassengersSpecification OrderByName()
    {
        Spec.AddSorting(passenger => passenger.FirstName);
        Spec.AddSorting(passenger => passenger.LastName);
        Spec.AddSorting(passenger => passenger.Id);

        return this;
    }
}
