using System.Security.Cryptography;
using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;
using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Application.TripImports.Preview;

public sealed class PreviewTripImportHandler(
    IEnumerable<ITripImportFileParser> parsers,
    ITripImportLookup lookup,
    IRepository repository
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

        IReadOnlyList<ParsedTripImportRow> parsedRows;
        try
        {
            parsedRows = await parser.ParseAsync(command.Content, ct);
        }
        catch (TripImportParseException exception)
        {
            return Result.Invalid(new ValidationError { Identifier = "file", ErrorMessage = exception.Message });
        }

        var duplicateTripNumbers = parsedRows
            .Where(row => !string.IsNullOrWhiteSpace(row.TripNumber))
            .GroupBy(row => row.TripNumber!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingPassengers = (await lookup.FindPassengersAsync(
            parsedRows.Where(row => !string.IsNullOrWhiteSpace(row.BrokerMemberId))
                .Select(row => row.BrokerMemberId!).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), ct
        )).ToDictionary(passenger => passenger.BrokerMemberId!, StringComparer.OrdinalIgnoreCase);
        var existingTrips = (await lookup.FindTripsAsync(
            parsedRows.Where(row => !string.IsNullOrWhiteSpace(row.TripNumber))
                .Select(row => row.TripNumber!).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), ct
        )).ToDictionary(trip => trip.BrokerTripNumber, StringComparer.OrdinalIgnoreCase);
        var import = TripImportAggregate.Create(
            command.FileName,
            fingerprint,
            parsedRows.Select(row => ToImportRow(
                row,
                duplicateTripNumbers.Contains(row.TripNumber ?? string.Empty),
                BelongsToDifferentPassenger(row, existingPassengers, existingTrips)
            ))
        );
        await repository.AddAsync(import, ct);
        return Result.Success(import.Id);
    }

    private static bool BelongsToDifferentPassenger(
        ParsedTripImportRow row,
        IReadOnlyDictionary<string, PassengerAggregate> passengers,
        IReadOnlyDictionary<string, TripAggregate> trips
    ) => !string.IsNullOrWhiteSpace(row.TripNumber)
         && trips.TryGetValue(row.TripNumber, out var trip)
         && (!string.IsNullOrWhiteSpace(row.BrokerMemberId)
             && (!passengers.TryGetValue(row.BrokerMemberId, out var passenger) || trip.PassengerId != passenger.Id));

    private static TripImportRow ToImportRow(
        ParsedTripImportRow row,
        bool hasDuplicateTripNumber,
        bool belongsToDifferentPassenger
    )
    {
        var messages = new List<string>();
        if (string.IsNullOrWhiteSpace(row.TripNumber)) messages.Add("Trip Number is required.");
        if (string.IsNullOrWhiteSpace(row.BrokerMemberId)) messages.Add("Medicaid Number is required.");
        if (row.AppointmentDateValidationError is not null) messages.Add(row.AppointmentDateValidationError);
        else if (row.ServiceDate is null) messages.Add("Appointment Date is required.");
        if (row.AppointmentTimeValidationError is not null) messages.Add(row.AppointmentTimeValidationError);
        if (string.IsNullOrWhiteSpace(row.FirstName)) messages.Add("Member's First Name is required.");
        if (string.IsNullOrWhiteSpace(row.LastName)) messages.Add("Member's Last Name is required.");
        if (hasDuplicateTripNumber) messages.Add("Trip Number occurs more than once in this import.");
        if (belongsToDifferentPassenger) messages.Add($"Trip {row.TripNumber} already belongs to a different passenger.");

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
