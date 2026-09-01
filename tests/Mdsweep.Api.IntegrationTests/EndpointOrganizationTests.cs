using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Mdsweep.Api.IntegrationTests;

public sealed class EndpointOrganizationTests : MdsweepIntegrationTest
{
    [Fact]
    public void Identity_and_dispatch_routes_are_registered_once_with_expected_authorization()
    {
        var dataSource = Application.Services.GetRequiredService<EndpointDataSource>();
        var routeEndpoints = dataSource.Endpoints.OfType<RouteEndpoint>().ToList();
        var identityEndpoints = routeEndpoints
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/auth/") == true)
            .ToDictionary(endpoint => endpoint.RoutePattern.RawText!);

        Assert.Equal(5, identityEndpoints.Count);
        Assert.True(IsAnonymous(identityEndpoints["/api/auth/login"]));
        AssertProtected(identityEndpoints["/api/auth/me"]);
        AssertProtected(identityEndpoints["/api/auth/tenant-context"]);
        AssertProtected(identityEndpoints["/api/auth/antiforgery"]);
        AssertProtected(identityEndpoints["/api/auth/logout"]);

        var expectedRoutes = new[]
        {
            "GET /api/auth/login",
            "GET /api/auth/me",
            "POST /api/auth/tenant-context",
            "GET /api/auth/antiforgery",
            "POST /api/auth/logout",
            "GET /api/drivers",
            "POST /api/drivers",
            "POST /api/drivers/access",
            "POST /api/drivers/{driverId:guid}/reset-access",
            "POST /api/drivers/{driverId:guid}/deactivate",
            "GET /api/vehicles",
            "POST /api/vehicles",
            "POST /api/vehicles/{vehicleId:guid}/deactivate",
            "POST /api/journeys/{journeyKey}/assignments",
            "POST /api/trips/{tripNumber}/assignments",
            "GET /api/trips/{tripNumber}/assignments",
            "GET /api/trips/{id:guid}",
            "PUT /api/trips/{id:guid}/scheduled-pickup-time",
            "GET /api/service-days/{serviceDate}/trips",
        };
        var expectedPaths = expectedRoutes
            .Select(route => route[(route.IndexOf(' ') + 1)..])
            .ToHashSet(StringComparer.Ordinal);
        var actualRoutes = routeEndpoints
            .Where(endpoint => expectedPaths.Contains(endpoint.RoutePattern.RawText!))
            .SelectMany(endpoint =>
                endpoint
                    .Metadata.GetRequiredMetadata<HttpMethodMetadata>()
                    .HttpMethods.Select(method => $"{method} {endpoint.RoutePattern.RawText}")
            )
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expectedRoutes.Order(StringComparer.Ordinal), actualRoutes);
    }

    private static bool IsAnonymous(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
        || endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0;

    private static void AssertProtected(RouteEndpoint endpoint) =>
        Assert.NotEmpty(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
}
