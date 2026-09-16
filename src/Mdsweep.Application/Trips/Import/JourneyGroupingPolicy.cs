using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Import;

public static class JourneyGroupingPolicy
{
    public static IReadOnlyDictionary<string, JourneyGroupingDecision> Group(
        IReadOnlyCollection<JourneyGroupingCandidate> newTrips,
        IReadOnlyCollection<TripAggregate> existingTrips,
        IReadOnlyCollection<JourneyAggregate> existingJourneys
    )
    {
        var journeysById = existingJourneys.ToDictionary(journey => journey.Id);
        var existingJourneySizes = existingTrips
            .GroupBy(trip => trip.JourneyId)
            .ToDictionary(group => group.Key, group => group.Count());

        var candidates = existingTrips
            .Where(trip =>
                existingJourneySizes[trip.JourneyId] == 1
                && journeysById[trip.JourneyId].GroupingType == JourneyGroupingType.Automatic
            )
            .Select(trip => new Candidate(
                trip.BrokerTripNumber,
                trip.PassengerId,
                trip.BrokerData,
                trip.JourneyId,
                IsNew: false
            ))
            .Concat(
                newTrips.Select(trip => new Candidate(
                    trip.TripNumber,
                    trip.PassengerId,
                    trip.BrokerData,
                    JourneyId: null,
                    IsNew: true
                ))
            )
            .ToArray();

        var matches = candidates.ToDictionary(
            candidate => candidate.Key,
            candidate =>
                candidates.Where(other => other.Key != candidate.Key && IsReciprocal(candidate, other)).ToArray()
        );

        var decisions = newTrips.ToDictionary(
            trip => trip.TripNumber,
            trip => new JourneyGroupingDecision(ExistingJourneyId: null, PairedNewTripNumber: null)
        );

        foreach (var trip in newTrips.OrderBy(trip => trip.TripNumber, StringComparer.Ordinal))
        {
            if (decisions[trip.TripNumber].HasMatch || matches[trip.TripNumber].Length != 1)
            {
                continue;
            }

            var match = matches[trip.TripNumber][0];

            if (matches[match.Key].Length != 1)
            {
                continue;
            }

            if (match.IsNew)
            {
                if (decisions[match.Key].HasMatch)
                {
                    continue;
                }

                decisions[trip.TripNumber] = new JourneyGroupingDecision(null, match.Key);
                decisions[match.Key] = new JourneyGroupingDecision(null, trip.TripNumber);
            }
            else
            {
                decisions[trip.TripNumber] = new JourneyGroupingDecision(match.JourneyId, null);
            }
        }

        return decisions;
    }

    private static bool IsReciprocal(Candidate first, Candidate second) =>
        first.PassengerId == second.PassengerId
        && first.BrokerData.ServiceDate == second.BrokerData.ServiceDate
        && first.BrokerData.Direction != second.BrokerData.Direction
        && NormalizeAddress(
            first.BrokerData.PickupAddress,
            first.BrokerData.PickupCity,
            first.BrokerData.PickupState,
            first.BrokerData.PickupZip
        )
            == NormalizeAddress(
                second.BrokerData.DropoffAddress,
                second.BrokerData.DropoffCity,
                second.BrokerData.DropoffState,
                second.BrokerData.DropoffZip
            )
        && NormalizeAddress(
            first.BrokerData.DropoffAddress,
            first.BrokerData.DropoffCity,
            first.BrokerData.DropoffState,
            first.BrokerData.DropoffZip
        )
            == NormalizeAddress(
                second.BrokerData.PickupAddress,
                second.BrokerData.PickupCity,
                second.BrokerData.PickupState,
                second.BrokerData.PickupZip
            );

    private static string NormalizeAddress(params string?[] parts) =>
        string.Join('|', parts.Select(part => NormalizePart(part ?? string.Empty)));

    private static string NormalizePart(string value) =>
        string.Join(' ', value.Trim().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private sealed record Candidate(
        string Key,
        Guid PassengerId,
        BrokerTripData BrokerData,
        Guid? JourneyId,
        bool IsNew
    );
}

public sealed record JourneyGroupingCandidate(string TripNumber, Guid PassengerId, BrokerTripData BrokerData);

public sealed record JourneyGroupingDecision(Guid? ExistingJourneyId, string? PairedNewTripNumber)
{
    public bool HasMatch => ExistingJourneyId.HasValue || PairedNewTripNumber is not null;
}
