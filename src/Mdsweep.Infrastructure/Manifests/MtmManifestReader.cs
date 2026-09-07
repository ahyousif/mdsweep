using System.Globalization;
using Mdsweep.Application.Trips.Import.Manifest;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Infrastructure.Manifests;

internal sealed class MtmManifestReader : IMtmManifestReader
{
    private static readonly string[] _dateFormats = ["M/d/yyyy", "MM/dd/yyyy", "M/d/yy", "yyyy-MM-dd"];
    private static readonly string[] _requiredHeaders =
    [
        "Trip Number",
        "Medicaid Number",
        "Member's First Name",
        "Member's Last Name",
        "Appointment Date",
        "Trip Type",
        "Pickup Address",
        "Pickup City",
        "Delivery Address",
        "Delivery City",
    ];

    public Task<MtmManifestReadResult> ReadAsync(string fileName, ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        var extension = Path.GetExtension(fileName);

        var table = extension.ToLowerInvariant() switch
        {
            ".csv" => ReadCsv(content),
            ".xlsx" => ReadExcel(content),
            _ => throw new NotSupportedException($"The file type '{extension}' is not supported."),
        };

        return Task.FromResult(ReadManifest(table));
    }

    private static List<IReadOnlyList<string>> ReadCsv(ReadOnlyMemory<byte> content)
    {
        using var stream = new MemoryStream(content.ToArray());
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);

        using var csv = new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null }
        );

        if (!csv.Read() || !csv.ReadHeader())
        {
            return [];
        }

        var headers = csv.HeaderRecord ?? [];
        var rows = new List<IReadOnlyList<string>> { headers };

        while (csv.Read())
        {
            var row = Enumerable.Range(0, headers.Length).Select(index => csv.GetField(index) ?? string.Empty).ToList();

            rows.Add(row);
        }

        return rows;
    }

    private static List<IReadOnlyList<string>> ReadExcel(ReadOnlyMemory<byte> content)
    {
        using var stream = new MemoryStream(content.ToArray());
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            return [];
        }

        var firstRow = worksheet.FirstRowUsed();

        if (firstRow is null)
        {
            return [];
        }

        var lastColumn = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        if (lastColumn == 0)
        {
            return [];
        }

        return
        [
            .. worksheet
                .RowsUsed()
                .Select(row =>
                    (IReadOnlyList<string>)
                        [
                            .. Enumerable
                                .Range(1, lastColumn)
                                .Select(column => worksheet.Cell(row.RowNumber(), column).GetFormattedString()),
                        ]
                ),
        ];
    }

    private static MtmManifestReadResult ReadManifest(IReadOnlyList<IReadOnlyList<string>> table)
    {
        if (table.Count == 0)
        {
            return new MtmManifestReadResult([], [new MtmManifestProblem(null, null, null, "The manifest is empty.")]);
        }

        var headers = ReadHeaders(table[0]);

        var missingHeaders = _requiredHeaders.Where(header => !headers.ContainsKey(header)).ToList();

        if (missingHeaders.Count > 0)
        {
            return new MtmManifestReadResult(
                [],
                [
                    new MtmManifestProblem(
                        null,
                        null,
                        null,
                        $"The manifest is missing required columns: {string.Join(", ", missingHeaders)}."
                    ),
                ]
            );
        }

        var problems = new List<MtmManifestProblem>();

        for (var index = 1; index < table.Count; index++)
        {
            var row = table[index];
            var rowNumber = index + 1;

            var tripNumber = Identifier(Cell(row, headers, "Trip Number"));

            var memberId = Identifier(Cell(row, headers, "Medicaid Number"));

            var firstName = Cell(row, headers, "Member's First Name");

            var lastName = Cell(row, headers, "Member's Last Name");

            var pickupAddress = Cell(row, headers, "Pickup Address");

            var pickupCity = Cell(row, headers, "Pickup City");

            var dropoffAddress = Cell(row, headers, "Delivery Address");

            var dropoffCity = Cell(row, headers, "Delivery City");

            var serviceDate = ParseDate(Cell(row, headers, "Appointment Date"), rowNumber, tripNumber, problems);

            var direction = ParseDirection(Cell(row, headers, "Trip Type"), rowNumber, tripNumber, problems);

            Require(problems, rowNumber, tripNumber, "TripNumber", tripNumber, "Trip number is required.");

            Require(problems, rowNumber, tripNumber, "MemberId", memberId, "Medicaid number is required.");

            Require(problems, rowNumber, tripNumber, "FirstName", firstName, "Member first name is required.");

            Require(problems, rowNumber, tripNumber, "LastName", lastName, "Member last name is required.");

            Require(problems, rowNumber, tripNumber, "PickupAddress", pickupAddress, "Pickup address is required.");

            Require(problems, rowNumber, tripNumber, "PickupCity", pickupCity, "Pickup city is required.");

            Require(problems, rowNumber, tripNumber, "DropoffAddress", dropoffAddress, "Delivery address is required.");

            Require(problems, rowNumber, tripNumber, "DropoffCity", dropoffCity, "Delivery city is required.");
        }

        return new MtmManifestReadResult([], []);
    }

    private static Dictionary<string, int> ReadHeaders(IReadOnlyList<string> row)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < row.Count; index++)
        {
            var header = row[index].Trim();

            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            headers.TryAdd(header, index);
        }

        return headers;
    }

    private static string? Cell(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headers, string header)
    {
        if (!headers.TryGetValue(header, out var index) || index >= row.Count)
        {
            return null;
        }

        var value = row[index].Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static LocalDate? ParseDate(
        string? value,
        int rowNumber,
        string? tripNumber,
        List<MtmManifestProblem> problems
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add(new MtmManifestProblem(rowNumber, tripNumber, "ServiceDate", "Appointment date is required."));

            return null;
        }

        foreach (var format in _dateFormats)
        {
            var pattern = LocalDatePattern.CreateWithInvariantCulture(format);

            var result = pattern.Parse(value);

            if (result.Success)
            {
                return result.Value;
            }
        }

        problems.Add(
            new MtmManifestProblem(rowNumber, tripNumber, "ServiceDate", $"Appointment date '{value}' is invalid.")
        );

        return null;
    }

    private static TripDirection? ParseDirection(
        string? value,
        int rowNumber,
        string? tripNumber,
        List<MtmManifestProblem> problems
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add(new MtmManifestProblem(rowNumber, tripNumber, "Direction", "Trip type is required."));

            return null;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "T" => TripDirection.To,
            "F" => TripDirection.From,
            _ => InvalidDirection(),
        };

        TripDirection? InvalidDirection()
        {
            problems.Add(
                new MtmManifestProblem(rowNumber, tripNumber, "Direction", $"Trip type '{value}' is not recognized.")
            );

            return null;
        }
    }

    private static void Require(
        List<MtmManifestProblem> problems,
        int rowNumber,
        string? tripNumber,
        string field,
        string? value,
        string message
    )
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        problems.Add(new MtmManifestProblem(rowNumber, tripNumber, field, message));
    }

    private static string? Identifier(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
