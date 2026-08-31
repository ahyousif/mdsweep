using System.Text.Json;
using Mdsweep.Application.ManifestImports;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.ManifestImports;

public static class GetManifestReceiptHandler
{
    public static async Task<GetManifestReceiptResult> Handle(
        GetManifestReceipt query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var receipt = await db
            .ManifestReceipts.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == query.ReceiptId,
                cancellationToken
            );
        if (receipt is null)
        {
            return new GetManifestReceiptResult(false, null);
        }

        var rows = JsonSerializer.Deserialize<List<ManifestReceiptRow>>(receipt.RowsJson) ?? [];
        return new GetManifestReceiptResult(
            true,
            new ManifestReceiptResponse(
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
            )
        );
    }
}
