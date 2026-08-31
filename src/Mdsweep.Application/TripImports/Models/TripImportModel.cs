using Mdsweep.Domain.TripImports;

namespace Mdsweep.Application.TripImports;

public sealed record TripImportModel(
    Guid Id,
    string FileName,
    TripImportStatus Status,
    Instant? AppliedAt,
    IReadOnlyList<TripImportRowModel> Rows
)
{
    public static TripImportModel FromAggregate(TripImportAggregate tripImport) =>
        new(
            tripImport.Id,
            tripImport.FileName,
            tripImport.Status,
            tripImport.AppliedAt,
            tripImport.Rows.Select(TripImportRowModel.FromEntity).ToList()
        );
}

public sealed record TripImportRowModel(
    int RowNumber,
    string? TripNumber,
    string? BrokerMemberId,
    TripImportRowDisposition Disposition,
    IReadOnlyList<string> Messages,
    DateOnly? ServiceDate,
    LocalTime? AppointmentTime
)
{
    public static TripImportRowModel FromEntity(TripImportRow row) =>
        new(row.RowNumber, row.TripNumber, row.BrokerMemberId, row.Disposition, row.Messages, row.ServiceDate, row.AppointmentTime);
}
