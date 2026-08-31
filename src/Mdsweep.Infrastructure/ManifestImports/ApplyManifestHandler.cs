using System.Text.Json;
using Mdsweep.Application.ManifestImports;
using Mdsweep.Domain.ManifestImports;
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
        var preview = await db.ManifestPreviews.SingleOrDefaultAsync(
            x => x.Id == command.PreviewId && x.ProviderId == command.ProviderId,
            cancellationToken
        );
        if (preview is null)
        {
            return new ApplyManifestResult(false, 0, 0);
        }

        var rows = JsonSerializer.Deserialize<List<ManifestPreviewRow>>(preview.RowsJson) ?? [];
        var importable = rows.Where(x => x.Disposition.IsImportable()).ToArray();
        if (preview.AppliedAt.HasValue)
        {
            return new ApplyManifestResult(true, importable.Length, rows.Count - importable.Length);
        }

        var tripNumbers = importable.Select(row => row.TripNumber).ToArray();
        var existing = await db
            .Trips.Where(x =>
                x.ProviderId == command.ProviderId && tripNumbers.Contains(x.TripNumber)
            )
            .ToDictionaryAsync(
                x => x.TripNumber,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken
            );

        foreach (var row in importable)
        {
            if (!existing.TryGetValue(row.TripNumber, out var trip))
            {
                trip = new Trip
                {
                    TripNumber = row.TripNumber,
                    ProviderId = command.ProviderId,
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
                    ProviderId = command.ProviderId,
                    ManifestPreviewId = preview.Id,
                    TripNumber = row.TripNumber,
                    AppointmentDate = row.AppointmentDate!.Value,
                    AppointmentTime = row.AppointmentTime!.Value,
                    PickupAddress = row.PickupAddress,
                    DeliveryAddress = row.DeliveryAddress,
                    BrokerStatus = row.BrokerStatus,
                }
            );
        }

        preview.AppliedAt ??= DateTimeOffset.UtcNow;

        return new ApplyManifestResult(true, importable.Length, rows.Count - importable.Length);
    }

    private static string JourneyKey(string tripNumber) =>
        tripNumber.Length > 1 && (tripNumber.EndsWith('A') || tripNumber.EndsWith('B'))
            ? tripNumber[..^1]
            : tripNumber;
}
