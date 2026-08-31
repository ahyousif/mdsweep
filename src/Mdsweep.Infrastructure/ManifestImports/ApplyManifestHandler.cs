using System.Text.Json;
using Mdsweep.Application.ManifestImports;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Domain.Passengers;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.ManifestImports;

public static class ApplyManifestHandler
{
    [Transactional]
    public static async Task<ApplyManifestResult> Handle(
        ApplyManifest command,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var receipt = await db.ManifestReceipts.SingleOrDefaultAsync(
            x => x.Id == command.ReceiptId,
            cancellationToken
        );
        if (receipt is null)
        {
            return new ApplyManifestResult(false, 0, 0);
        }

        var rows = JsonSerializer.Deserialize<List<ManifestReceiptRow>>(receipt.RowsJson) ?? [];
        var importable = rows.Where(x => x.Disposition.IsImportable()).ToArray();
        if (receipt.AppliedAt.HasValue)
        {
            return new ApplyManifestResult(true, importable.Length, rows.Count - importable.Length);
        }

        var tripNumbers = importable.Select(row => row.TripNumber).ToArray();
        var existing = await db
            .Trips.Where(x => tripNumbers.Contains(x.TripNumber))
            .ToDictionaryAsync(
                x => x.TripNumber,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken
            );

        var brokerMemberIds = importable
            .Select(row => row.BrokerMemberId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var passengers = await db
            .Passengers.Where(passenger => brokerMemberIds.Contains(passenger.BrokerMemberId!))
            .ToDictionaryAsync(passenger => passenger.BrokerMemberId!, StringComparer.Ordinal, cancellationToken);

        foreach (var row in importable)
        {
            if (!passengers.TryGetValue(row.BrokerMemberId, out var passenger))
            {
                passenger = PassengerAggregate.Create(
                    row.BrokerMemberId,
                    row.MemberFirstName,
                    row.MemberLastName
                );
                db.Passengers.Add(passenger);
                passengers.Add(row.BrokerMemberId, passenger);
            }

            if (!existing.TryGetValue(row.TripNumber, out var trip))
            {
                trip = new Trip
                {
                    TripNumber = row.TripNumber,
                    PassengerId = passenger.Id,
                    JourneyKey = JourneyKey(row.TripNumber),
                };
                db.Trips.Add(trip);
                existing.Add(row.TripNumber, trip);
            }

            trip.ReconcileBrokerFields(row);
            db.TripBrokerImports.Add(
                new TripBrokerImport
                {
                    TripId = trip.Id,
                    ManifestReceiptId = receipt.Id,
                    TripNumber = row.TripNumber,
                    BrokerMemberId = row.BrokerMemberId,
                    AppointmentDate = row.AppointmentDate!.Value,
                    AppointmentTime = row.AppointmentTime!.Value,
                    PickupAddress = row.PickupAddress,
                    DeliveryAddress = row.DeliveryAddress,
                    BrokerStatus = row.BrokerStatus,
                }
            );
        }

        receipt.AppliedAt ??= DateTimeOffset.UtcNow;

        return new ApplyManifestResult(true, importable.Length, rows.Count - importable.Length);
    }

    private static string JourneyKey(string tripNumber) =>
        tripNumber.Length > 1 && (tripNumber.EndsWith('A') || tripNumber.EndsWith('B'))
            ? tripNumber[..^1]
            : tripNumber;
}
