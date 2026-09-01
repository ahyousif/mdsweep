using Mdsweep.Application.TripImports.Abstractions;

namespace Mdsweep.Application.TripImports.Get;

public sealed class GetTripImportHandler(ITripImportLookup lookup)
{
    public async Task<Result<TripImportModel>> Handle(GetTripImportQuery query, CancellationToken ct)
    {
        var tripImport = await lookup.FindImportAsync(query.Id, ct);

        if (tripImport is null)
        {
            return Result.NotFound();
        }

        return Result.Success(TripImportModel.FromAggregate(tripImport));
    }
}
