using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Routing;

namespace Mdsweep.Api.IntegrationTests;

public sealed class EndpointOrganizationTests : MdsweepIntegrationTest
{
    [Fact]
    public void Oidc_authentication_persists_the_id_token_needed_for_Keycloak_sign_out()
    {
        var oidc = Application.Services
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        Assert.True(oidc.SaveTokens);
    }

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
            "POST /api/passengers",
            "POST /api/trip-imports",
            "GET /api/trip-imports/{id:guid}",
            "POST /api/trip-imports/{id:guid}/apply",
            "GET /api/trips/{id:guid}",
            "GET /api/trips",
            "PUT /api/trips/{id:guid}/scheduled-pickup-time",
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

        Assert.Equal(expectedRoutes.Order(StringComparer.Ordinal), actualRoutes.Order(StringComparer.Ordinal));
    }

    private static bool IsAnonymous(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
        || endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0;

    private static void AssertProtected(RouteEndpoint endpoint) =>
        Assert.NotEmpty(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
}
