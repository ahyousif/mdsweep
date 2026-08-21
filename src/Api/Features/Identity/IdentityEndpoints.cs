using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Mdsweep.Api.Infrastructure;

namespace Mdsweep.Api.Features.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentity(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/auth/login", () => Results.Challenge(
            new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]));
        endpoints.MapGet("/api/auth/me", async (ClaimsPrincipal user, ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            var contexts = await ProviderContextResolver.ResolveAll(user, db, cancellationToken);
            return contexts.Count == 0
                ? Results.Forbid()
                : Results.Ok(contexts.Select(context => new { appUserId = context.AppUserId, providerId = context.ProviderId, role = context.Role }));
        }).RequireAuthorization();
        endpoints.MapPost("/api/auth/provider-context", SelectProviderContext).RequireAuthorization();
        endpoints.MapGet("/api/auth/antiforgery", (IAntiforgery antiforgery, HttpContext httpContext) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            return Results.Ok(new { token = tokens.RequestToken });
        }).RequireAuthorization();
        endpoints.MapPost("/api/auth/logout", () => Results.SignOut(
            properties: null, authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> SelectProviderContext(
        SelectProviderContextRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var context = (await ProviderContextResolver.ResolveAll(user, db, cancellationToken))
            .SingleOrDefault(x => x.ProviderId == request.ProviderId);
        if (context is null) return Results.Forbid();

        var identity = new ClaimsIdentity(user.Identity);
        foreach (var claim in identity.FindAll(ProviderContextResolver.ActiveProviderIdClaim).ToArray())
            identity.RemoveClaim(claim);
        identity.AddClaim(new Claim(ProviderContextResolver.ActiveProviderIdClaim, context.ProviderId.ToString()));
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.NoContent();
    }

    private sealed record SelectProviderContextRequest(Guid ProviderId);
}
