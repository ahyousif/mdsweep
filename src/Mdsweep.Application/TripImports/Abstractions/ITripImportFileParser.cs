namespace Mdsweep.Application.TripImports.Abstractions;

public interface ITripImportFileParser
{
    bool CanParse(string fileName, string? contentType);

    Task<IReadOnlyList<ParsedTripImportItem>> ParseAsync(
        ReadOnlyMemory<byte> content,
        CancellationToken ct
    );
}
