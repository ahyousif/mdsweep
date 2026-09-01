using System.Security.Cryptography;
using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Trips;

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
            return Result.Invalid(
                new ValidationError { Identifier = "file", ErrorMessage = "A non-empty CSV or XLSX file is required." }
            );
        }

        var parser = parsers.SingleOrDefault(candidate => candidate.CanParse(command.FileName, command.ContentType));
        if (parser is null)
        {
            return Result.Invalid(
                new ValidationError
                {
                    Identifier = "file",
                    ErrorMessage = "Only CSV and XLSX trip-import files are supported.",
                }
            );
        }

        var fingerprint = Convert.ToHexString(SHA256.HashData(command.Content));

        IReadOnlyList<ParsedTripImportItem> parsedItems;
        try
        {
            parsedItems = await parser.ParseAsync(command.Content, ct);
        }
        catch (TripImportParseException exception)
        {
            return Result.Invalid(new ValidationError { Identifier = "file", ErrorMessage = exception.Message });
        }

        var duplicateTripNumbers = parsedItems
            .Where(item => !string.IsNullOrWhiteSpace(item.TripNumber))
            .GroupBy(item => item.TripNumber!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingPassengers = (
            await lookup.FindPassengersAsync(
                parsedItems
                    .Where(item => !string.IsNullOrWhiteSpace(item.BrokerMemberId))
                    .Select(item => item.BrokerMemberId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                ct
            )
        ).ToDictionary(passenger => passenger.BrokerMemberId!, StringComparer.OrdinalIgnoreCase);
        var existingTrips = (
            await lookup.FindTripsAsync(
                parsedItems
                    .Where(item => !string.IsNullOrWhiteSpace(item.TripNumber))
                    .Select(item => item.TripNumber!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                ct
            )
        ).ToDictionary(trip => trip.BrokerTripNumber, StringComparer.OrdinalIgnoreCase);

        var import = TripImportAggregate.Create(
            command.FileName,
            fingerprint,
            parsedItems.Select(item =>
                ToImportItem(
                    item,
                    duplicateTripNumbers.Contains(item.TripNumber ?? string.Empty),
                    BelongsToDifferentPassenger(item, existingPassengers, existingTrips)
                )
            )
        );

        await repository.AddAsync(import, ct);

        return Result.Success(import.Id);
    }

    private static bool BelongsToDifferentPassenger(
        ParsedTripImportItem item,
        IReadOnlyDictionary<string, PassengerAggregate> passengers,
        IReadOnlyDictionary<string, TripAggregate> trips
    ) =>
        !string.IsNullOrWhiteSpace(item.TripNumber)
        && trips.TryGetValue(item.TripNumber, out var trip)
        && (
            !string.IsNullOrWhiteSpace(item.BrokerMemberId)
            && (!passengers.TryGetValue(item.BrokerMemberId, out var passenger) || trip.PassengerId != passenger.Id)
        );

    private static TripImportItem ToImportItem(
        ParsedTripImportItem item,
        bool hasDuplicateTripNumber,
        bool belongsToDifferentPassenger
    )
    {
        var messages = new List<string>();
        if (string.IsNullOrWhiteSpace(item.TripNumber))
            messages.Add("Trip Number is required.");
        if (string.IsNullOrWhiteSpace(item.BrokerMemberId))
            messages.Add("Medicaid Number is required.");
        if (item.AppointmentDateValidationError is not null)
            messages.Add(item.AppointmentDateValidationError);
        else if (item.ServiceDate is null)
            messages.Add("Appointment Date is required.");
        if (item.AppointmentTimeValidationError is not null)
            messages.Add(item.AppointmentTimeValidationError);
        if (string.IsNullOrWhiteSpace(item.FirstName))
            messages.Add("Member's First Name is required.");
        if (string.IsNullOrWhiteSpace(item.LastName))
            messages.Add("Member's Last Name is required.");
        if (hasDuplicateTripNumber)
            messages.Add("Trip Number occurs more than once in this import.");
        if (belongsToDifferentPassenger)
            messages.Add($"Trip {item.TripNumber} already belongs to a different passenger.");

        var disposition =
            messages.Count > 0 ? TripImportItemDisposition.Blocked
            : item.AppointmentTime is null && !item.IsWillCall ? TripImportItemDisposition.Warning
            : TripImportItemDisposition.Ready;

        if (disposition is TripImportItemDisposition.Warning)
            messages.Add("Appointment Time is missing; dispatcher scheduling is required.");

        return new TripImportItem(
            item.RowNumber,
            item.TripNumber,
            item.BrokerMemberId,
            item.FirstName,
            item.LastName,
            item.ServiceDate,
            item.AppointmentTime,
            item.PickupAddress,
            item.PickupCity,
            item.DropoffAddress,
            item.DropoffCity,
            item.BrokerStatus,
            item.IsWillCall,
            disposition,
            messages
        );
    }
}
