using Mdsweep.Application.TripImports.Abstractions;

namespace Mdsweep.Application.TripImports.Get;

public sealed class GetTripImportHandler(ITripImportLookup lookup)
{
    public async Task<Result<TripImportModel>> Handle(GetTripImportQuery query, CancellationToken ct)
    {
        var tripImport = await lookup.FindImportAsync(query.Id, ct);
        return tripImport is null ? Result.NotFound() : Result.Success(TripImportModel.FromAggregate(tripImport));
    }
}
