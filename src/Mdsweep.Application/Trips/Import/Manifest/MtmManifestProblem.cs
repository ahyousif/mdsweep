namespace Mdsweep.Application.Trips.Import.Manifest;

public sealed record MtmManifestProblem(int? RowNumber, string? TripNumber, string? Field, string Message);
