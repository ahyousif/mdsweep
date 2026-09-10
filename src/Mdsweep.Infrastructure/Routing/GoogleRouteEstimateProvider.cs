using Ardalis.Result;
using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Infrastructure.Http;

namespace Mdsweep.Infrastructure.Routing;

// TODO: revisit this implementation
public sealed class GoogleRouteEstimateProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<GoogleRoutesOptions> options,
    ILogger<GoogleRouteEstimateProvider> logger
) : IRouteEstimateProvider
{
    public const string HttpClientName = "GoogleRoutes";

    private const string RouteEndpoint = "directions/v2:computeRoutes";
    private const string ApiKeyHeader = "X-Goog-Api-Key";
    private const string FieldMaskHeader = "X-Goog-FieldMask";
    private const string EstimateFieldMask = "routes.duration,routes.distanceMeters";

    public async Task<Result<RouteEstimate>> GetEstimateAsync(string origin, string destination, CancellationToken ct)
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
            .WithHeader(FieldMaskHeader, EstimateFieldMask)
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

            return Result<RouteEstimate>.Error("Unable to calculate a route for the supplied addresses.");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));

        if (!TryGetEstimate(document.RootElement, out var estimate))
        {
            return Result<RouteEstimate>.Error("Google Routes did not return a route estimate.");
        }

        return Result.Success(estimate);
    }

    private static bool TryGetEstimate(JsonElement root, out RouteEstimate estimate)
    {
        estimate = default!;

        if (
            !root.TryGetProperty("routes", out var routes)
            || routes.ValueKind != JsonValueKind.Array
            || routes.GetArrayLength() == 0
        )
        {
            return false;
        }

        var route = routes[0];

        if (
            !route.TryGetProperty("duration", out var durationElement)
            || !route.TryGetProperty("distanceMeters", out var distanceMetersElement)
        )
        {
            return false;
        }

        var value = durationElement.GetString();

        if (
            string.IsNullOrWhiteSpace(value)
            || !value.EndsWith('s')
            || !double.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            || !distanceMetersElement.TryGetInt32(out var distanceMeters)
        )
        {
            return false;
        }

        estimate = new RouteEstimate(Duration.FromSeconds(seconds), distanceMeters);

        return true;
    }
}
