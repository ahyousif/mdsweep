using Ardalis.Result;
using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Infrastructure.Http;

namespace Mdsweep.Infrastructure.Routing;

internal sealed class GoogleRouteDurationProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<GoogleRoutesOptions> options,
    ILogger<GoogleRouteDurationProvider> logger
) : IRouteDurationProvider
{
    public const string HttpClientName = "GoogleRoutes";

    private const string RouteEndpoint = "directions/v2:computeRoutes";

    private const string ApiKeyHeader = "X-Goog-Api-Key";

    private const string FieldMaskHeader = "X-Goog-FieldMask";

    private const string DurationFieldMask = "routes.duration";

    public async Task<Result<Duration>> GetDurationAsync(string origin, string destination, CancellationToken ct)
    {
        Guard.Against.NullOrWhiteSpace(origin);
        Guard.Against.NullOrWhiteSpace(destination);

        using var response = await new HttpRequestBuilder(httpClientFactory, HttpClientName)
            .WithUrl(RouteEndpoint)
            .WithMethod(HttpMethod.Post)
            .WithBody(
                new
                {
                    origin = new { address = origin },
                    destination = new { address = destination },
                    travelMode = "DRIVE",
                    computeAlternativeRoutes = false,
                }
            )
            .WithHeader(ApiKeyHeader, options.Value.ApiKey!)
            .WithHeader(FieldMaskHeader, DurationFieldMask)
            .WithSuccessRequired(false)
            .SendAsync(ct);

        if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
        {
            throw new HttpRequestException(
                $"Google Routes returned {(int)response.StatusCode}.",
                null,
                response.StatusCode
            );
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Google Routes could not calculate route from {Origin} to {Destination}. Status code: {StatusCode}",
                origin,
                destination,
                (int)response.StatusCode
            );

            return Result<Duration>.Error("Unable to calculate a route for the supplied addresses.");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));

        if (!TryGetDuration(document.RootElement, out var duration))
        {
            return Result<Duration>.Error("Google Routes did not return a travel duration.");
        }

        return Result.Success(duration);
    }

    private static bool TryGetDuration(JsonElement root, out Duration duration)
    {
        duration = default;

        if (
            !root.TryGetProperty("routes", out var routes)
            || routes.ValueKind != JsonValueKind.Array
            || routes.GetArrayLength() == 0
        )
        {
            return false;
        }

        var route = routes[0];

        if (!route.TryGetProperty("duration", out var durationElement))
        {
            return false;
        }

        var value = durationElement.GetString();

        if (
            string.IsNullOrWhiteSpace(value)
            || !value.EndsWith('s')
            || !double.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
        )
        {
            return false;
        }

        duration = Duration.FromSeconds(seconds);

        return true;
    }
}
