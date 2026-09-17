using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Trips.Specifications;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Import;

public static class JourneyGroupingReconciler
{
    public static async Task<IReadOnlyDictionary<string, Guid>> ReconcileAsync(
        IReadOnlyCollection<JourneyGroupingCandidate> newTrips,
        IReadOnlyCollection<TripAggregate> groupingChangedTrips,
        IRepository repository,
        CancellationToken ct
    )
    {
        if (newTrips.Count == 0 && groupingChangedTrips.Count == 0)
        {
            return new Dictionary<string, Guid>();
        }

        var affectedJourneyIds = groupingChangedTrips.Select(trip => trip.JourneyId).Distinct().ToArray();
        var affectedJourneys = affectedJourneyIds.Length == 0
            ? []
            : await repository.ListAsync(new JourneysSpecification().WithIds(affectedJourneyIds).Build(), ct);
        var affectedAutomaticIds = affectedJourneys
            .Where(journey => journey.GroupingType == JourneyGroupingType.Automatic)
            .Select(journey => journey.Id)
            .ToHashSet();
        var affectedTrips = affectedAutomaticIds.Count == 0
            ? []
            : await repository.ListAsync(
                new TripsSpecification().WithJourneyIds(affectedAutomaticIds.ToArray()).Build(),
                ct
            );

        var searchTrips = newTrips
            .Concat(affectedTrips.Select(trip => Candidate(trip)))
            .ToArray();
        if (searchTrips.Length == 0)
        {
            return new Dictionary<string, Guid>();
        }

        var nearbyTrips = await repository.ListAsync(
            new TripsSpecification()
                .WithPassengerIds(searchTrips.Select(trip => trip.PassengerId).Distinct().ToArray())
                .WithServiceDates(searchTrips.Select(trip => trip.BrokerData.ServiceDate).Distinct().ToArray())
                .Build(),
            ct
        );
        var candidateJourneyIds = nearbyTrips
            .Select(trip => trip.JourneyId)
            .Concat(affectedAutomaticIds)
            .Distinct()
            .ToArray();
        var journeys = candidateJourneyIds.Length == 0
            ? []
            : await repository.ListAsync(new JourneysSpecification().WithIds(candidateJourneyIds).Build(), ct);
        var journeyTrips = candidateJourneyIds.Length == 0
            ? []
            : await repository.ListAsync(new TripsSpecification().WithJourneyIds(candidateJourneyIds).Build(), ct);
        var journeysById = journeys.ToDictionary(journey => journey.Id);
        var membersByJourneyId = journeyTrips
            .GroupBy(trip => trip.JourneyId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var changedIds = groupingChangedTrips.Select(trip => trip.Id).ToHashSet();

        var brokenJourneyIds = affectedAutomaticIds
            .Where(journeyId =>
            {
                var members = membersByJourneyId[journeyId];
                return members.Length > 1
                    && (members.Length != 2 || JourneyGroupingPolicy.FindPairs(members.Select(Candidate).ToArray()).Count != 2);
            })
            .ToHashSet();
        var freeTrips = journeyTrips
            .Where(trip =>
                journeysById[trip.JourneyId].GroupingType == JourneyGroupingType.Automatic
                && (membersByJourneyId[trip.JourneyId].Length == 1 || brokenJourneyIds.Contains(trip.JourneyId))
            )
            .ToArray();
        var originalFreeJourneyIds = freeTrips.Select(trip => trip.JourneyId).Distinct().ToArray();
        var freeTripsByNumber = freeTrips.ToDictionary(trip => trip.BrokerTripNumber);
        var allCandidates = freeTrips.Select(Candidate).Concat(newTrips).ToArray();
        var pairs = JourneyGroupingPolicy.FindPairs(allCandidates);
        var activeNumbers = newTrips.Select(trip => trip.TripNumber)
            .Concat(
                freeTrips
                    .Where(trip => brokenJourneyIds.Contains(trip.JourneyId) || changedIds.Contains(trip.Id))
                    .Select(trip => trip.BrokerTripNumber)
            )
            .ToHashSet();

        var groups = new List<List<string>>();
        var groupByNumber = new Dictionary<string, int>();
        foreach (var candidate in allCandidates.OrderBy(trip => trip.TripNumber, StringComparer.Ordinal))
        {
            if (groupByNumber.ContainsKey(candidate.TripNumber))
            {
                continue;
            }

            var group = new List<string> { candidate.TripNumber };
            if (pairs.TryGetValue(candidate.TripNumber, out var partner)
                && (activeNumbers.Contains(candidate.TripNumber) || activeNumbers.Contains(partner)))
            {
                group.Add(partner);
            }

            var groupIndex = groups.Count;
            groups.Add(group);
            foreach (var number in group)
            {
                groupByNumber.Add(number, groupIndex);
            }
        }

        var groupJourneyIds = new Guid?[groups.Count];
        for (var index = 0; index < groups.Count; index++)
        {
            var singleton = groups[index]
                .Where(freeTripsByNumber.ContainsKey)
                .Select(number => freeTripsByNumber[number])
                .Where(trip => membersByJourneyId[trip.JourneyId].Length == 1)
                .OrderBy(trip => activeNumbers.Contains(trip.BrokerTripNumber))
                .ThenBy(trip => trip.BrokerTripNumber, StringComparer.Ordinal)
                .FirstOrDefault();
            groupJourneyIds[index] = singleton?.JourneyId;
        }

        foreach (var journeyId in brokenJourneyIds.Order())
        {
            foreach (var member in membersByJourneyId[journeyId]
                .OrderBy(trip => changedIds.Contains(trip.Id))
                .ThenBy(trip => trip.BrokerTripNumber, StringComparer.Ordinal))
            {
                var groupIndex = groupByNumber[member.BrokerTripNumber];
                if (groupJourneyIds[groupIndex] is null)
                {
                    groupJourneyIds[groupIndex] = journeyId;
                    break;
                }
            }
        }

        for (var index = 0; index < groups.Count; index++)
        {
            if (groupJourneyIds[index] is not null)
            {
                continue;
            }

            var journey = JourneyAggregate.Create(JourneyGroupingType.Automatic);
            groupJourneyIds[index] = journey.Id;
            journeysById.Add(journey.Id, journey);
            await repository.AddAsync(journey, ct);
        }

        foreach (var trip in freeTrips)
        {
            var destinationId = groupJourneyIds[groupByNumber[trip.BrokerTripNumber]]!.Value;
            if (trip.JourneyId == destinationId)
            {
                continue;
            }

            trip.RegroupAutomatically(journeysById[trip.JourneyId], journeysById[destinationId]);
            await repository.UpdateAsync(trip, ct);
        }

        var retainedJourneyIds = groupJourneyIds.Select(id => id!.Value).ToHashSet();
        foreach (var oldJourneyId in originalFreeJourneyIds)
        {
            if (!retainedJourneyIds.Contains(oldJourneyId))
            {
                await repository.DeleteAsync(journeysById[oldJourneyId], ct);
            }
        }

        return newTrips.ToDictionary(trip => trip.TripNumber, trip => groupJourneyIds[groupByNumber[trip.TripNumber]]!.Value);
    }

    private static JourneyGroupingCandidate Candidate(TripAggregate trip) =>
        new(trip.BrokerTripNumber, trip.PassengerId, trip.BrokerData);
}
