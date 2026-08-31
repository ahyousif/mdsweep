using Mdsweep.Domain.ManifestImports;

namespace Mdsweep.Application.ManifestImports;

public sealed record ReceiveManifest(string FileName, string Extension, byte[] Content);

public sealed record ApplyManifest(Guid ReceiptId);

public sealed record ApplyManifestResult(bool Found, int Imported, int Blocked);

public sealed record GetManifestReceipt(Guid ReceiptId);

public sealed record GetManifestReceiptResult(bool Found, ManifestReceiptResponse? Receipt);

public sealed record ManifestReceiptResponse(
    Guid ReceiptId,
    int Ready,
    int Warning,
    int Blocked,
    IReadOnlyList<DateOnly> ServiceDates,
    IReadOnlyList<ManifestReceiptRow> Rows
);
