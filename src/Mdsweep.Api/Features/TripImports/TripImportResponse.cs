using Mdsweep.Application.TripImports;
using Mdsweep.Domain.TripImports;

namespace Mdsweep.Api.Features.TripImports;

public sealed record TripImportResponse(
    Guid Id,
    string FileName,
    TripImportStatus Status,
    Instant? AppliedAt,
    IReadOnlyList<TripImportRowResponse> Rows
)
{
    public static TripImportResponse FromModel(TripImportModel model) =>
        new(model.Id, model.FileName, model.Status, model.AppliedAt, model.Rows.Select(TripImportRowResponse.FromModel).ToList());
}

public sealed record TripImportRowResponse(
    int RowNumber,
    string? TripNumber,
    string? BrokerMemberId,
    TripImportRowDisposition Disposition,
    IReadOnlyList<string> Messages,
    DateOnly? ServiceDate,
    LocalTime? AppointmentTime
)
{
    public static TripImportRowResponse FromModel(TripImportRowModel model) =>
        new(model.RowNumber, model.TripNumber, model.BrokerMemberId, model.Disposition, model.Messages, model.ServiceDate, model.AppointmentTime);
}
