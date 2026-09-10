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
}
