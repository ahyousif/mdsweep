using Mdsweep.Domain.ManifestImports;

namespace Mdsweep.Application.ManifestImports;

public sealed record PreviewManifest(
    string TenantId,
    string FileName,
    string Extension,
    byte[] Content
);

public sealed record ApplyManifest(string TenantId, Guid PreviewId);

public sealed record ApplyManifestResult(bool Found, int Imported, int Blocked);

public sealed record GetManifestPreview(string TenantId, Guid PreviewId);

public sealed record GetManifestPreviewResult(bool Found, ManifestPreviewResponse? Preview);

public sealed record ManifestPreviewResponse(
    Guid PreviewId,
    int Ready,
    int Warning,
    int Blocked,
    IReadOnlyList<DateOnly> ServiceDates,
    IReadOnlyList<ManifestPreviewRow> Rows
);
