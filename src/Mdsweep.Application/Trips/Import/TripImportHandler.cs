using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Passengers.Specifications;
using Mdsweep.Application.Trips.Import.Manifest;
using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Application.Trips.Specifications;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Import;

public sealed class TripImportHandler(IMtmManifestReader manifestReader, IRepository repository)
{
    public async Task<(Result<TripImportSummary> Result, OutgoingMessages Messages)> Handle(
        TripImportCommand command,
        CancellationToken ct
    )
    {
        var manifest = await manifestReader.ReadAsync(command.FileName, command.Content, ct);

        var rows = manifest.Rows;

        if (rows.Count == 0)
        {
            return (
                Result.Success(
                    new TripImportSummary(
                        ReadyCount: 0,
                        NeedsAttentionCount: manifest
                            .Problems.Where(x => x.RowNumber.HasValue)
                            .Select(x => x.RowNumber)
                            .Distinct()
                            .Count(),
                        Problems: manifest.Problems
                    )
                ),
                []
            );
        }

        var tripNumbers = rows.Select(row => row.TripNumber).Distinct().ToArray();

        var memberIds = rows.Select(row => row.MemberId).Distinct().ToArray();

        var existingTrips = await repository.ListAsync(
            new TripsSpecification().WithTripNumbers(tripNumbers).Build(),
            ct
        );

        var existingPassengers = await repository.ListAsync(
            new PassengersSpecification().WithBrokerMemberIds(memberIds).Build(),
            ct
        );

        var tripsByNumber = existingTrips.ToDictionary(trip => trip.BrokerTripNumber);

        var passengersByMemberId = existingPassengers
            .Where(passenger => passenger.BrokerMemberId is not null)
            .ToDictionary(passenger => passenger.BrokerMemberId!);

        var problems = manifest.Problems.ToList();
        var readyCount = 0;
        var newTrips = new List<JourneyGroupingCandidate>();
        var groupingChangedTrips = new List<TripAggregate>();

        var duplicateTripNumbers = rows.GroupBy(row => row.TripNumber)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        var outgoingMessages = new OutgoingMessages();

        foreach (var row in rows)
        {
            if (duplicateTripNumbers.Contains(row.TripNumber))
            {
                problems.Add(
                    new MtmManifestProblem(
                        row.RowNumber,
                        row.TripNumber,
                        "TripNumber",
                        "Trip number appears more than once in this manifest.",
                        "manifest.duplicateTrip"
                    )
                );
            }
            else
            {
                tripsByNumber.TryGetValue(row.TripNumber, out var existingTrip);

                passengersByMemberId.TryGetValue(row.MemberId, out var passenger);

                if (existingTrip is not null)
                {
                    if (passenger is null || existingTrip.PassengerId != passenger.Id)
                    {
                        problems.Add(
                            new MtmManifestProblem(
                                row.RowNumber,
                                row.TripNumber,
                                "MemberId",
                                "This trip is already assigned to a different passenger.",
                                "manifest.passengerMismatch"
                            )
                        );
                    }
                    else
                    {
                        await ReconcilePassengerAsync(passenger, row, repository, ct);

                        var brokerData = ToBrokerTripData(row);

                        if (existingTrip.BrokerData != brokerData)
                        {
                            if (JourneyGroupingPolicy.GroupingFactsChanged(existingTrip.BrokerData, brokerData))
                            {
                                groupingChangedTrips.Add(existingTrip);
                            }

                            existingTrip.UpdateBrokerData(brokerData);

                            await repository.UpdateAsync(existingTrip, ct);

                            if (existingTrip.RequiresRouteEstimate)
                            {
                                outgoingMessages.Add(new ScheduleTripCommand(existingTrip.Id));
                            }
                        }

                        readyCount++;
                    }
                }
                else
                {
                    if (passenger is null)
                    {
                        passenger = PassengerAggregate.Create(
                            row.MemberId,
                            row.FirstName,
                            row.LastName,
                            row.DateOfBirth,
                            row.PhoneNumber,
                            row.AlternatePhoneNumber,
                            row.PassengerType,
                            row.SpecialNeeds
                        );

                        await repository.AddAsync(passenger, ct);

                        passengersByMemberId.Add(row.MemberId, passenger);
                    }
                    else
                    {
                        await ReconcilePassengerAsync(passenger, row, repository, ct);
                    }

                    newTrips.Add(new JourneyGroupingCandidate(row.TripNumber, passenger.Id, ToBrokerTripData(row)));
                }
            }
        }

        var journeyIds = await JourneyGroupingReconciler.ReconcileAsync(newTrips, groupingChangedTrips, repository, ct);
        foreach (var candidate in newTrips)
        {
            var trip = TripAggregate.Create(
                journeyIds[candidate.TripNumber],
                candidate.PassengerId,
                candidate.TripNumber,
                candidate.BrokerData
            );

            await repository.AddAsync(trip, ct);

            if (trip.RequiresRouteEstimate)
            {
                outgoingMessages.Add(new ScheduleTripCommand(trip.Id));
            }

            readyCount++;
        }

        var summary = new TripImportSummary(
            ReadyCount: readyCount,
            NeedsAttentionCount: problems
                .Where(problem => problem.RowNumber.HasValue)
                .Select(problem => problem.RowNumber)
                .Distinct()
                .Count(),
            Problems: problems
        );

        return (Result.Success(summary), outgoingMessages);
    }

    private static async Task ReconcilePassengerAsync(
        PassengerAggregate passenger,
        MtmManifestRow row,
        IRepository repository,
        CancellationToken ct
    )
    {
        if (!passenger.UpdateMtmDetails(
            row.FirstName,
            row.LastName,
            row.DateOfBirth,
            row.PhoneNumber,
            row.AlternatePhoneNumber,
            row.PassengerType,
            row.SpecialNeeds
        ))
        {
            return;
        }

        await repository.UpdateAsync(passenger, ct);
    }

    private static BrokerTripData ToBrokerTripData(MtmManifestRow row)
    {
        return new BrokerTripData(
            ServiceDate: row.ServiceDate,
            AppointmentTime: row.Direction == TripDirection.To ? row.Time : null,
            BrokerPickupTime: row.Direction == TripDirection.From && !row.IsWillCall ? row.Time : null,
            Direction: row.Direction,
            IsWillCall: row.IsWillCall,
            PickupAddress: row.PickupAddress,
            PickupCity: row.PickupCity,
            PickupState: row.PickupState,
            PickupZip: row.PickupZip,
            DropoffAddress: row.DropoffAddress,
            DropoffCity: row.DropoffCity,
            DropoffState: row.DropoffState,
            DropoffZip: row.DropoffZip,
            Status: row.BrokerStatus,
            PassengerType: row.PassengerType,
            SpecialNeeds: row.SpecialNeeds,
            Cost: row.TripCost,
            Mileage: row.TripMileage
        );
    }
}
