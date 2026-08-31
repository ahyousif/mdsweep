using System.Text.Json;
using Mdsweep.Application.ManifestImports;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.ManifestImports;

public static class GetManifestPreviewHandler
{
    public static async Task<GetManifestPreviewResult> Handle(
        GetManifestPreview query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var preview = await db
            .ManifestPreviews.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == query.PreviewId && x.TenantId == query.TenantId,
                cancellationToken
            );
        if (preview is null)
        {
            return new GetManifestPreviewResult(false, null);
        }

        var rows = JsonSerializer.Deserialize<List<ManifestPreviewRow>>(preview.RowsJson) ?? [];
        return new GetManifestPreviewResult(
            true,
            new ManifestPreviewResponse(
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
            )
        );
    }
}
