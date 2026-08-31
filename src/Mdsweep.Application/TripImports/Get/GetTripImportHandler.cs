using Mdsweep.Application.TripImports.Abstractions;

namespace Mdsweep.Application.TripImports.Get;

public sealed class GetTripImportHandler(ITripImportWorkflowStore store)
{
    public async Task<Result<TripImportModel>> Handle(GetTripImportQuery query, CancellationToken ct)
    {
        var tripImport = await store.FindImportAsync(query.Id, ct);
        return tripImport is null ? Result.NotFound() : Result.Success(TripImportModel.FromAggregate(tripImport));
    }
}
