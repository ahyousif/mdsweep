namespace Mdsweep.Application.TripImports.Import;

public sealed record ImportTripsResult(
    string FileName,
    int Total,
    int Added,
    int Updated,
    int Unchanged,
    int ProblemCount,
    IReadOnlyList<TripImportProblem> Problems
)
{
    public IReadOnlyList<Guid> SchedulingTripIds { get; init; } = [];
}

public sealed record TripImportProblem(int RowNumber, string? TripNumber, string Message);
