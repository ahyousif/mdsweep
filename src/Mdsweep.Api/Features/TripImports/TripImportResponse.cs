using Mdsweep.Application.TripImports;
using Mdsweep.Domain.TripImports;

namespace Mdsweep.Api.Features.TripImports;

public sealed record TripImportResponse(
    Guid Id,
    string FileName,
    TripImportStatus Status,
    Instant? AppliedAt,
    IReadOnlyList<TripImportItemResponse> Items
)
{
    public static TripImportResponse FromModel(TripImportModel model) =>
        new(model.Id, model.FileName, model.Status, model.AppliedAt, model.Items.Select(TripImportItemResponse.FromModel).ToList());
}

public sealed record TripImportItemResponse(
    int RowNumber,
    string? TripNumber,
    string? BrokerMemberId,
    TripImportItemDisposition Disposition,
    IReadOnlyList<string> Messages,
    DateOnly? ServiceDate,
    LocalTime? AppointmentTime
)
{
    public static TripImportItemResponse FromModel(TripImportItemModel model) =>
        new(model.RowNumber, model.TripNumber, model.BrokerMemberId, model.Disposition, model.Messages, model.ServiceDate, model.AppointmentTime);
}
