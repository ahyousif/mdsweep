using System.Security.Claims;

namespace Mdsweep.Api.Features.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentity(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/auth/login",
            () =>
                Results.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    [OpenIdConnectDefaults.AuthenticationScheme]
                )
        );
        endpoints
            .MapGet(
                "/api/auth/me",
                async (ClaimsPrincipal user, ITenantAccess tenantAccess, CancellationToken cancellationToken) =>
                {
                    var userSubject = user.FindFirstValue("sub");
                    if (string.IsNullOrWhiteSpace(userSubject))
                        return Results.Forbid();

                    var contexts = await tenantAccess.GetMembershipsAsync(userSubject, cancellationToken);
                    return contexts.Count == 0
                        ? Results.Forbid()
                        : Results.Ok(
                            contexts.Select(context => new
                            {
                                userId = context.UserId,
                                tenantId = context.TenantId,
                                role = context.Role,
                            })
                        );
                }
            )
            .RequireAuthorization();
        endpoints.MapPost("/api/auth/tenant-context", SelectTenantContext).RequireAuthorization();
        endpoints
            .MapGet(
                "/api/auth/antiforgery",
                (IAntiforgery antiforgery, HttpContext httpContext) =>
                {
                    var tokens = antiforgery.GetAndStoreTokens(httpContext);
                    httpContext.Response.Cookies.Append(
                        AuthenticationConventions.AntiforgeryRequestCookieName,
                        tokens.RequestToken!,
                        new CookieOptions
                        {
                            HttpOnly = false,
                            SameSite = SameSiteMode.Strict,
                            Secure = httpContext.Request.IsHttps,
                            Path = "/",
                        }
                    );
                    return Results.Ok(new { token = tokens.RequestToken });
                }
            )
            .RequireAuthorization();
        endpoints
            .MapPost(
                "/api/auth/logout",
                () =>
                    Results.SignOut(
                        properties: null,
                        authenticationSchemes:
                        [
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            OpenIdConnectDefaults.AuthenticationScheme,
                        ]
                    )
            )
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> SelectTenantContext(
        SelectTenantContextRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ITenantAccess tenantAccess,
        CancellationToken cancellationToken
    )
    {
        var userSubject = user.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userSubject))
            return Results.Forbid();

        var context = (await tenantAccess.GetMembershipsAsync(userSubject, cancellationToken)).SingleOrDefault(x =>
            x.TenantId == request.TenantId
        );
        if (context is null)
            return Results.Forbid();

        var identity = new ClaimsIdentity(user.Identity);
        foreach (var claim in identity.FindAll(TenantClaimTypes.ActiveTenantId).ToArray())
            identity.RemoveClaim(claim);
        identity.AddClaim(new Claim(TenantClaimTypes.ActiveTenantId, context.TenantId));
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.NoContent();
    }

    /// <summary>Selects the Tenant used for subsequent authenticated requests.</summary>
    private sealed record SelectTenantContextRequest(string TenantId);
}
