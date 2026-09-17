namespace Mdsweep.Domain.Trips;

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
            .Select(trip => new JourneyGroupingCandidate(trip.BrokerTripNumber, trip.PassengerId, trip.BrokerData))
            .Concat(newTrips)
            .ToArray();
        var pairs = FindPairs(candidates);
        var newTripNumbers = newTrips.Select(trip => trip.TripNumber).ToHashSet();
        var existingJourneyByTripNumber = existingTrips.ToDictionary(trip => trip.BrokerTripNumber, trip => trip.JourneyId);

        var decisions = newTrips.ToDictionary(
            trip => trip.TripNumber,
            trip => new JourneyGroupingDecision(ExistingJourneyId: null, PairedNewTripNumber: null)
        );

        foreach (var trip in newTrips)
        {
            if (!pairs.TryGetValue(trip.TripNumber, out var partnerNumber))
            {
                continue;
            }

            if (newTripNumbers.Contains(partnerNumber))
            {
                decisions[trip.TripNumber] = new JourneyGroupingDecision(null, partnerNumber);
            }
            else
            {
                decisions[trip.TripNumber] = new JourneyGroupingDecision(existingJourneyByTripNumber[partnerNumber], null);
            }
        }

        return decisions;
    }

    public static bool GroupingFactsChanged(BrokerTripData previous, BrokerTripData current) =>
        Facts(previous) != Facts(current);

    public static IReadOnlyDictionary<string, string> FindPairs(IReadOnlyCollection<JourneyGroupingCandidate> trips)
    {
        var pairs = new Dictionary<string, string>();

        foreach (var partition in trips.GroupBy(trip => (trip.PassengerId, trip.BrokerData.ServiceDate)))
        {
            var candidates = partition
                .Select(trip => new Candidate(trip.TripNumber, Facts(trip.BrokerData)))
                .ToArray();
            var matches = candidates.ToDictionary(
                candidate => candidate.TripNumber,
                candidate => candidates.Where(other => other.TripNumber != candidate.TripNumber && IsReciprocal(candidate, other)).ToArray()
            );

            foreach (var candidate in candidates)
            {
                if (matches[candidate.TripNumber].Length != 1)
                {
                    continue;
                }

                var partner = matches[candidate.TripNumber][0];
                if (matches[partner.TripNumber].Length == 1)
                {
                    pairs[candidate.TripNumber] = partner.TripNumber;
                }
            }
        }

        return pairs;
    }

    private static bool IsReciprocal(Candidate first, Candidate second) =>
        first.Facts.Direction != second.Facts.Direction
        && first.Facts.Pickup == second.Facts.Dropoff
        && first.Facts.Dropoff == second.Facts.Pickup;

    private static GroupingFacts Facts(BrokerTripData data) =>
        new(
            data.ServiceDate,
            data.Direction,
            NormalizeAddress(data.PickupAddress, data.PickupCity, data.PickupState, data.PickupZip),
            NormalizeAddress(data.DropoffAddress, data.DropoffCity, data.DropoffState, data.DropoffZip)
        );

    private static string NormalizeAddress(params string?[] parts) =>
        string.Join('|', parts.Select(part => NormalizePart(part ?? string.Empty)));

    private static string NormalizePart(string value) =>
        string.Join(' ', value.Trim().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private sealed record GroupingFacts(LocalDate ServiceDate, TripDirection Direction, string Pickup, string Dropoff);

    private sealed record Candidate(string TripNumber, GroupingFacts Facts);
}

public sealed record JourneyGroupingCandidate(string TripNumber, Guid PassengerId, BrokerTripData BrokerData);

public sealed record JourneyGroupingDecision(Guid? ExistingJourneyId, string? PairedNewTripNumber)
{
    public bool HasMatch => ExistingJourneyId.HasValue || PairedNewTripNumber is not null;
}
