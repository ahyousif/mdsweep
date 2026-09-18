using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Mdsweep.Api.IntegrationTests;

public sealed class EndpointOrganizationTests : MdsweepIntegrationTest
{
    [Fact]
    public void Oidc_authentication_persists_the_id_token_needed_for_Keycloak_sign_out()
    {
        var oidc = Application
            .Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        Assert.True(oidc.SaveTokens);
    }

    [Fact]
    public void Api_routes_are_registered_once()
    {
        var routes = GetApiRoutes();

        var duplicates = routes
            .GroupBy(route => (route.Method, route.Path))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.Method} {group.Key.Path}")
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Api_routes_require_authorization_except_known_public_endpoints()
    {
        var routes = GetApiRoutes();

        var publicRoutes = new HashSet<(string Method, string Path)>
        {
            ("GET", "/api/auth/login"),
            ("GET", "/api/auth/register"),
        };

        foreach (var route in routes)
        {
            if (publicRoutes.Contains((route.Method, route.Path)))
            {
                Assert.True(IsUnprotected(route.Endpoint), $"Expected {route.Method} {route.Path} to be anonymous.");
            }
            else
            {
                Assert.True(
                    IsProtected(route.Endpoint),
                    $"Expected {route.Method} {route.Path} to require authorization."
                );
            }
        }
    }

    private IReadOnlyList<RegisteredRoute> GetApiRoutes()
    {
        var dataSource = Application.Services.GetRequiredService<EndpointDataSource>();

        return
        [
            .. dataSource
                .Endpoints.OfType<RouteEndpoint>()
                .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true)
                .SelectMany(endpoint =>
                {
                    var httpMethods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;

                    if (httpMethods is null)
                        return [];

                    return httpMethods.Select(method => new RegisteredRoute(
                        method,
                        endpoint.RoutePattern.RawText!,
                        endpoint
                    ));
                }),
        ];
    }

    private static bool IsProtected(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0
        && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;

    private static bool IsUnprotected(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
        || endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0;

    private sealed record RegisteredRoute(string Method, string Path, RouteEndpoint Endpoint);
}
