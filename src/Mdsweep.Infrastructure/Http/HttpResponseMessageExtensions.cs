namespace Mdsweep.Infrastructure.Http;

public static class HttpResponseMessageExtensions
{
    public static async Task EnsureSuccessWithBodyAsync(
        this HttpResponseMessage response,
        string? context,
        CancellationToken ct = default
    )
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync(ct);

        var label = string.IsNullOrWhiteSpace(context) ? "Request" : context.Trim();

        throw new HttpRequestException(
            $"{label} failed with status {(int)response.StatusCode} ({response.StatusCode}). Body: {body}",
            null,
            response.StatusCode
        );
    }
}
