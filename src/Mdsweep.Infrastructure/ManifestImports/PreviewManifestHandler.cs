using Mdsweep.Application.ManifestImports;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Dispatch;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace Mdsweep.Infrastructure.ManifestImports;

public static class PreviewManifestHandler
{
    [Transactional]
    public static async Task<ManifestPreviewResponse> Handle(
        PreviewManifest command,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        await using var stream = new MemoryStream(command.Content, writable: false);
        var parsedRows = command.Extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? await ManifestCsv.Preview(stream, cancellationToken)
            : ManifestXlsx.Preview(stream);
        var rows = await IdentifyBrokerChanges(
            parsedRows,
            command.ProviderId,
            db,
            cancellationToken
        );
        var preview = new ManifestPreview
        {
            FileName = command.FileName,
            ProviderId = command.ProviderId,
            RowsJson = System.Text.Json.JsonSerializer.Serialize(rows),
        };

        db.ManifestPreviews.Add(preview);

        return new ManifestPreviewResponse(
            preview.Id,
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

    private static async Task<IReadOnlyList<ManifestPreviewRow>> IdentifyBrokerChanges(
        IReadOnlyList<ManifestPreviewRow> rows,
        Guid providerId,
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
            .Where(trip => trip.ProviderId == providerId && tripNumbers.Contains(trip.TripNumber))
            .ToDictionaryAsync(
                trip => trip.TripNumber,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken
            );
        var scheduledTripIds = await DispatchReadModel.GetTripIdsWithProviderOverrides(
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
                var hasProviderOverrides = scheduledTripIds.Contains(trip.Id);
                var messages =
                    differences.Count > 0
                        ? row.Messages.Append($"MTM changed: {string.Join(", ", differences)}.")
                        : row.Messages;
                if (hasProviderOverrides)
                {
                    messages = messages.Append("Your scheduled pickup time will be preserved.");
                }

                return row with
                {
                    BrokerChange =
                        differences.Count == 0
                            ? ManifestBrokerChange.Unchanged
                            : ManifestBrokerChange.BrokerChanged,
                    HasProviderOverrides = hasProviderOverrides,
                    IsActive = row.Disposition.IsActive(),
                    Messages = messages.ToArray(),
                };
            })
            .ToArray();
    }
}
