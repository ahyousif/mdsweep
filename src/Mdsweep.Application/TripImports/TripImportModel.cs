using Mdsweep.Domain.TripImports;

namespace Mdsweep.Application.TripImports;

public sealed record TripImportModel(
    Guid Id,
    string FileName,
    TripImportStatus Status,
    Instant? AppliedAt,
    IReadOnlyList<TripImportItemModel> Items
)
{
    public static TripImportModel FromAggregate(TripImportAggregate tripImport) =>
        new(
            tripImport.Id,
            tripImport.FileName,
            tripImport.Status,
            tripImport.AppliedAt,
            tripImport.Items.Select(TripImportItemModel.FromEntity).ToList()
        );
}

public sealed record TripImportItemModel(
    int RowNumber,
    string? TripNumber,
    string? BrokerMemberId,
    TripImportItemDisposition Disposition,
    IReadOnlyList<string> Messages,
    DateOnly? ServiceDate,
    LocalTime? AppointmentTime
)
{
    public static TripImportItemModel FromEntity(TripImportItem item) =>
        new(item.RowNumber, item.TripNumber, item.BrokerMemberId, item.Disposition, item.Messages, item.ServiceDate, item.AppointmentTime);
}
