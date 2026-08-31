using System.Security.Cryptography;
using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.TripImports;

namespace Mdsweep.Application.TripImports.Preview;

public sealed class PreviewTripImportHandler(
    IEnumerable<ITripImportFileParser> parsers,
    ITripImportWorkflowStore store
)
{
    public async Task<Result<Guid>> Handle(PreviewTripImportCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.FileName) || command.Content.Length == 0)
        {
            return Result.Invalid(new ValidationError { Identifier = "file", ErrorMessage = "A non-empty CSV or XLSX file is required." });
        }

        var parser = parsers.SingleOrDefault(candidate => candidate.CanParse(command.FileName, command.ContentType));
        if (parser is null)
        {
            return Result.Invalid(new ValidationError { Identifier = "file", ErrorMessage = "Only CSV and XLSX trip-import files are supported." });
        }

        var fingerprint = Convert.ToHexString(SHA256.HashData(command.Content));
        if (await store.HasContentFingerprintAsync(fingerprint, ct))
        {
            return Result.Conflict("This file has already been imported for this tenant.");
        }

        IReadOnlyList<ParsedTripImportRow> parsedRows;
        try
        {
            parsedRows = await parser.ParseAsync(command.Content, ct);
        }
        catch (TripImportParseException exception)
        {
            return Result.Invalid(new ValidationError { Identifier = "file", ErrorMessage = exception.Message });
        }

        var import = TripImportAggregate.Create(
            command.FileName,
            fingerprint,
            parsedRows.Select(ToImportRow)
        );
        await store.AddAsync(import, ct);
        return Result.Success(import.Id);
    }

    private static TripImportRow ToImportRow(ParsedTripImportRow row)
    {
        var messages = new List<string>();
        if (string.IsNullOrWhiteSpace(row.TripNumber)) messages.Add("Trip Number is required.");
        if (string.IsNullOrWhiteSpace(row.BrokerMemberId)) messages.Add("Medicaid Number is required.");
        if (row.ServiceDate is null) messages.Add("Appointment Date is required.");
        if (string.IsNullOrWhiteSpace(row.FirstName)) messages.Add("Member's First Name is required.");
        if (string.IsNullOrWhiteSpace(row.LastName)) messages.Add("Member's Last Name is required.");

        var disposition = messages.Count > 0
            ? TripImportRowDisposition.Blocked
            : row.AppointmentTime is null && !row.IsWillCall
                ? TripImportRowDisposition.Warning
                : TripImportRowDisposition.Ready;

        if (disposition is TripImportRowDisposition.Warning)
            messages.Add("Appointment Time is missing; dispatcher scheduling is required.");

        return new TripImportRow(
            row.RowNumber, row.TripNumber, row.BrokerMemberId, row.FirstName, row.LastName,
            row.ServiceDate, row.AppointmentTime, row.PickupAddress, row.PickupCity,
            row.DropoffAddress, row.DropoffCity, row.BrokerStatus, row.IsWillCall, disposition, messages
        );
    }
}
