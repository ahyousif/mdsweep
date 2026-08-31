using Mdsweep.Application.ManifestImports;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Dispatch;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.ManifestImports;

public static class ReceiveManifestHandler
{
    [Transactional]
    public static async Task<ManifestReceiptResponse> Handle(
        ReceiveManifest command,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        await using var stream = new MemoryStream(command.Content, writable: false);
        var parsedRows = command.Extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? await ManifestCsv.Preview(stream, cancellationToken)
            : ManifestXlsx.Preview(stream);
        var rows = await IdentifyBrokerChanges(parsedRows, db, cancellationToken);
        var receipt = new ManifestReceipt
        {
            FileName = command.FileName,
            RowsJson = System.Text.Json.JsonSerializer.Serialize(rows),
        };

        db.ManifestReceipts.Add(receipt);

        return new ManifestReceiptResponse(
            receipt.Id,
            rows.Count(x => x.Disposition == ManifestRowDisposition.Ready),
            rows.Count(x => x.Disposition == ManifestRowDisposition.Warning),
            rows.Count(x => x.Disposition == ManifestRowDisposition.Blocked),
            rows.Where(x => x.AppointmentDate.HasValue)
                .Select(x => x.AppointmentDate!.Value)
                .Distinct()
                .Order()
                .ToArray(),
            rows
        );
    }

    private static async Task<IReadOnlyList<ManifestReceiptRow>> IdentifyBrokerChanges(
        IReadOnlyList<ManifestReceiptRow> rows,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var tripNumbers = rows.Where(row => row.Disposition.IsImportable())
            .Select(row => row.TripNumber)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var existing = await db
            .Trips.AsNoTracking()
            .Where(trip => tripNumbers.Contains(trip.TripNumber))
            .ToDictionaryAsync(
                trip => trip.TripNumber,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken
            );
        var scheduledTripIds = await DispatchReadModel.GetTripIdsWithOperationalOverrides(
            db,
            existing.Values.Select(trip => trip.Id),
            cancellationToken
        );

        return rows.Select(row =>
            {
                if (!row.Disposition.IsImportable())
                {
                    return row with
                    {
                        BrokerChange = ManifestBrokerChange.Blocked,
                        IsActive = false,
                    };
                }

                if (!existing.TryGetValue(row.TripNumber, out var trip))
                {
                    return row with
                    {
                        BrokerChange = ManifestBrokerChange.New,
                        IsActive = row.Disposition.IsActive(),
                    };
                }

                var differences = trip.BrokerDifferences(row);
                var hasOperationalOverrides = scheduledTripIds.Contains(trip.Id);
                var messages =
                    differences.Count > 0
                        ? row.Messages.Append($"MTM changed: {string.Join(", ", differences)}.")
                        : row.Messages;
                if (hasOperationalOverrides)
                {
                    messages = messages.Append("Your scheduled pickup time will be preserved.");
                }

                return row with
                {
                    BrokerChange =
                        differences.Count == 0
                            ? ManifestBrokerChange.Unchanged
                            : ManifestBrokerChange.BrokerChanged,
                    HasOperationalOverrides = hasOperationalOverrides,
                    IsActive = row.Disposition.IsActive(),
                    Messages = messages.ToArray(),
                };
            })
            .ToArray();
    }
}
