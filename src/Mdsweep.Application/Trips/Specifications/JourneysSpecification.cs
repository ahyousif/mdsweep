using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Specifications;

public sealed class JourneysSpecification : SpecificationBuilder<JourneyAggregate, Guid, JourneysSpecification>
{
    public JourneysSpecification WithIds(IReadOnlyCollection<Guid> journeyIds)
    {
        Spec.Add(query => query.Where(journey => journeyIds.Contains(journey.Id)));
        return this;
    }
}
