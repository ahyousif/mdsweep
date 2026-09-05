using System.Security.Claims;
using Mdsweep.Api.Common.Authentication;

namespace Mdsweep.Api.Features.Identity;

public static class IdentityEndpoints
{
    private const string Route = "/api/auth";

    public static IEndpointRouteBuilder MapIdentity(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup(Route).RequireAuthorization();

        auth.MapGet("/login", Login).AllowAnonymous();

        auth.MapGet("/session", GetSession);

        auth.MapPost("/tenant-context", SelectTenantContext);

        auth.MapGet("/antiforgery", GetAntiforgeryToken);

        auth.MapPost("/logout", Logout);

        return endpoints;
    }

    private static IResult Login(string? returnUrl)
    {
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = LocalReturnUrl(returnUrl) },
            [OpenIdConnectDefaults.AuthenticationScheme]
        );
    }

    private static async Task<IResult> GetSession(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userSubject = user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userSubject))
        {
            return Results.Forbid();
        }

        var memberships = await tenantAccess.GetMembershipsAsync(userSubject, cancellationToken);

        if (memberships.Count == 0)
        {
            return Results.Forbid();
        }

        var tenants = memberships
            .GroupBy(membership => new { membership.TenantId, membership.TenantName })
            .Select(group => new TenantSessionResponse(
                group.Key.TenantId,
                group.Key.TenantName,
                group.Select(membership => membership.Role).Distinct().Order().ToArray()
            ))
            .OrderBy(tenant => tenant.Name)
            .ToArray();
        var activeTenantId = user.FindFirstValue(CustomClaimTypes.ActiveTenantId);
        var activeTenant = tenants.SingleOrDefault(tenant => tenant.Id == activeTenantId);

        StoreAntiforgeryRequestToken(antiforgery, httpContext);

        var firstMembership = memberships[0];
        return Results.Ok(new SessionResponse(
            firstMembership.UserId,
            $"{firstMembership.FirstName} {firstMembership.LastName}".Trim(),
            activeTenant,
            tenants
        ));
    }

    private static async Task<IResult> SelectTenantContext(
        SelectTenantContextRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ITenantAccess tenantAccess,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken
    )
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        var userSubject = user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userSubject))
        {
            return Results.Forbid();
        }

        var memberships = await tenantAccess.GetMembershipsAsync(userSubject, cancellationToken);

        var membership = memberships.SingleOrDefault(membership => membership.TenantId == request.TenantId);

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

    private static async Task<IResult> Logout(IAntiforgery antiforgery, HttpContext httpContext)
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        return Results.SignOut(
            properties: null,
            authenticationSchemes:
            [
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme,
            ]
        );
    }

    private sealed record SelectTenantContextRequest(string TenantId);

    private static string LocalReturnUrl(string? returnUrl)
    {
        if (
            string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith("/", StringComparison.Ordinal)
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.Contains('\\')
            || returnUrl.Contains("%2f", StringComparison.OrdinalIgnoreCase)
            || returnUrl.Contains("%5c", StringComparison.OrdinalIgnoreCase)
            || !Uri.TryCreate(returnUrl, UriKind.Relative, out _)
        )
        {
            return "/";
        }

        return returnUrl;
    }

    private static AntiforgeryTokenSet StoreAntiforgeryRequestToken(
        IAntiforgery antiforgery,
        HttpContext httpContext
    )
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

    private sealed record TenantSessionResponse(string Id, string Name, string[] Roles);

    private sealed record SessionResponse(
        Guid UserId,
        string DisplayName,
        TenantSessionResponse? ActiveTenant,
        TenantSessionResponse[] AvailableTenants
    );

    private sealed record AntiforgeryTokenResponse(string Token);
}
