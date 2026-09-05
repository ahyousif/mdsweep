using System.Net.Http.Json;
using Mdsweep.Application.Trips.Scheduling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mdsweep.Infrastructure.Trips.Scheduling;

public sealed class GoogleRoutesEstimator(
    HttpClient httpClient,
    IOptions<GoogleRoutesOptions> options,
    ILogger<GoogleRoutesEstimator> logger
) : IRouteEstimator
{
    public async Task<TimeSpan?> EstimateDurationAsync(RouteLocation pickup, RouteLocation dropoff, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            logger.LogWarning("Google Routes is not configured; route estimation was skipped.");
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "directions/v2:computeRoutes")
        {
            Content = JsonContent.Create(
                new
                {
                    origin = new { address = $"{pickup.Address}, {pickup.City}" },
                    destination = new { address = $"{dropoff.Address}, {dropoff.City}" },
                    travelMode = "DRIVE",
                    computeAlternativeRoutes = false,
                }
            ),
        };
        request.Headers.Add("X-Goog-Api-Key", options.Value.ApiKey);
        request.Headers.Add("X-Goog-FieldMask", "routes.duration");

        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Google Routes returned {StatusCode}", (int)response.StatusCode);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<Response>(cancellationToken: ct);
        return ParseDuration(result?.Routes?.FirstOrDefault()?.Duration);
    }

    private static TimeSpan? ParseDuration(string? value) =>
        value is not null && value.EndsWith('s') && double.TryParse(value[..^1], out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;

    private sealed record Response(Route[]? Routes);
    private sealed record Route(string? Duration);
}
