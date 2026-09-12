namespace Mdsweep.Api.Features.Identity;

public static class IdentityEndpoints
{
    private const string Route = "/api/auth";

    public static IEndpointRouteBuilder MapIdentity(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup(Route).RequireAuthorization();

        auth.MapAuthenticationEndpoints();
        auth.MapSessionEndpoints();

        return endpoints;
    }
}
