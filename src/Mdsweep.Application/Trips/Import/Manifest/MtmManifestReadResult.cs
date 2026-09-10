namespace Mdsweep.Application.Trips.Import.Manifest;

public sealed record MtmManifestReadResult(
    IReadOnlyList<MtmManifestRow> Rows,
    IReadOnlyList<MtmManifestProblem> Problems
);
