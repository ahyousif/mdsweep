using Mdsweep.Application.TripImports.Import;

namespace Mdsweep.Api.Features.Trips.Import;

public sealed record ImportTripsResponse(
    string FileName, int Total, int Added, int Updated, int Unchanged, int ProblemCount,
    IReadOnlyList<TripImportProblem> Problems
)
{
    public static ImportTripsResponse FromResult(ImportTripsResult result) =>
        new(result.FileName, result.Total, result.Added, result.Updated, result.Unchanged, result.ProblemCount, result.Problems);
}
