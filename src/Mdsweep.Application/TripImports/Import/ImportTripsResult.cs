namespace Mdsweep.Application.TripImports.Import;

public sealed record ImportTripsResult(
    string FileName,
    int Total,
    int Added,
    int Updated,
    int Unchanged,
    int NeedsAttention,
    IReadOnlyList<TripImportProblem> Problems
);

public sealed record TripImportProblem(int RowNumber, string? TripNumber, string Message);
