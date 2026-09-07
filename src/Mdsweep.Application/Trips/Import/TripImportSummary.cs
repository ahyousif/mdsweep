using Mdsweep.Application.Trips.Import.Manifest;

namespace Mdsweep.Application.Trips.Import;

public sealed record TripImportSummary(
    int ReadyCount,
    int NeedsAttentionCount,
    IReadOnlyList<MtmManifestProblem> Problems
);
