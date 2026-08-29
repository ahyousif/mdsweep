using Mdsweep.Domain.ManifestImports;

namespace Mdsweep.Infrastructure.ManifestImports;

public static class ManifestCsv
{
    public static async Task<IReadOnlyList<ManifestPreviewRow>> Preview(
        Stream source,
        CancellationToken cancellationToken
    )
    {
        using var reader = new StreamReader(source, leaveOpen: true);
        var records = Parse(await reader.ReadToEndAsync(cancellationToken));
        return ManifestTabular.Preview(records);
    }

    private static List<List<string>> Parse(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '"')
            {
                if (quoted && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                    quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                record.Add(field.ToString());
                field.Clear();
            }
            else if ((character == '\n' || character == '\r') && !quoted)
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    index++;
                record.Add(field.ToString());
                field.Clear();
                records.Add(record);
                record = [];
            }
            else
                field.Append(character);
        }
        if (field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }
        return records;
    }
}

public sealed class ManifestFormatException(string message) : Exception(message);
