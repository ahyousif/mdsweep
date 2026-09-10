using Mdsweep.Api.Common.Authentication;

namespace Mdsweep.Api.Features.Identity;

internal static class SessionEndpoints
{
    public static RouteGroupBuilder MapSessionEndpoints(this RouteGroupBuilder auth)
    {
        auth.MapGet("/session", GetSession);

        auth.MapPost("/tenant-context", SelectTenantContext);

        auth.MapGet("/antiforgery", GetAntiforgeryToken);

        return auth;
    }

    private static async Task<IResult> GetSession(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var subject = user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Results.Forbid();
        }

        var memberships = await tenantAccess.GetMembershipsAsync(subject, ct);

        var tenants = memberships
            .GroupBy(x => new { x.TenantId, x.TenantName })
            .Select(group => new TenantSessionResponse(
                group.Key.TenantId,
                group.Key.TenantName,
                [.. group.Select(x => x.Role).Distinct().Order()]
            ))
            .OrderBy(x => x.Name)
            .ToArray();

        var activeTenantId = user.FindFirstValue(CustomClaimTypes.ActiveTenantId);

        var activeTenant = tenants.SingleOrDefault(x => x.Id == activeTenantId);

        StoreAntiforgeryRequestToken(antiforgery, httpContext);

        var membership = memberships.FirstOrDefault();

        return Results.Ok(
            new SessionResponse(
                membership?.UserId,
                $"{membership?.FirstName} {membership?.LastName}".Trim(),
                activeTenant,
                tenants
            )
        );
    }

    private static async Task<IResult> SelectTenantContext(
        SelectTenantContextRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ITenantAccess tenantAccess,
        CancellationToken ct
    )
    {
        var subject = user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Results.Forbid();
        }

        var memberships = await tenantAccess.GetMembershipsAsync(subject, ct);

        var membership = memberships.FirstOrDefault(x => x.TenantId == request.TenantId);

        if (membership is null)
        {
            return Results.Forbid();
        }

        var identity = new ClaimsIdentity(user.Identity);

        foreach (var claim in identity.FindAll(CustomClaimTypes.ActiveTenantId).ToArray())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(CustomClaimTypes.ActiveTenantId, membership.TenantId));

        var authenticationResult = await httpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authenticationResult.Properties
        );

        return Results.NoContent();
    }

    private static IResult GetAntiforgeryToken(IAntiforgery antiforgery, HttpContext httpContext)
    {
        var tokens = StoreAntiforgeryRequestToken(antiforgery, httpContext);

        return Results.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!));
    }

    private static AntiforgeryTokenSet StoreAntiforgeryRequestToken(IAntiforgery antiforgery, HttpContext httpContext)
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

        return tokens;
    }

    private sealed record SelectTenantContextRequest(string TenantId);

    private sealed record TenantSessionResponse(string Id, string Name, string[] Roles);

    private sealed record SessionResponse(
        Guid? UserId,
        string DisplayName,
        TenantSessionResponse? ActiveTenant,
        TenantSessionResponse[] AvailableTenants
    );

    private sealed record AntiforgeryTokenResponse(string Token);
}
