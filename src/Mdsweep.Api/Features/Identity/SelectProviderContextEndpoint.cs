using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Identity;

public static class SelectProviderContextEndpoint
{
    [WolverinePost(IdentityRoutes.ProviderContext)]
    public static async Task<IResult> Post(
        SelectProviderContextRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = (
            await ProviderContextResolver.ResolveAll(user, bus, cancellationToken)
        ).SingleOrDefault(candidate => candidate.ProviderId == request.ProviderId);
        if (context is null)
            return Results.Forbid();

        var identity = new ClaimsIdentity(user.Identity);
        foreach (
            var claim in identity
                .FindAll(ProviderContextResolver.ActiveProviderIdClaim)
                .Concat(identity.FindAll(ClaimTypes.Role))
                .Concat(identity.FindAll("roles"))
                .ToArray()
        )
            identity.RemoveClaim(claim);

        identity.AddClaim(
            new Claim(ProviderContextResolver.ActiveProviderIdClaim, context.ProviderId.ToString())
        );
        identity.AddClaim(new Claim(ClaimTypes.Role, context.Role));
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity)
        );
        return Results.NoContent();
    }
}
