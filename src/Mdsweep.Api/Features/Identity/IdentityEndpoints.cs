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

        auth.MapGet("/me", GetCurrentUser);

        auth.MapPost("/tenant-context", SelectTenantContext);

        auth.MapGet("/antiforgery", GetAntiforgeryToken);

        auth.MapPost("/logout", Logout);

        return endpoints;
    }

    private static IResult Login()
    {
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = "/" },
            [OpenIdConnectDefaults.AuthenticationScheme]
        );
    }

    private static async Task<IResult> GetCurrentUser(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
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

        return Results.Ok(
            memberships.Select(membership => new TenantMembershipResponse(
                membership.UserId,
                membership.FirstName,
                membership.LastName,
                membership.TenantId,
                membership.Role
            ))
        );
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

    private sealed record TenantMembershipResponse(
        Guid UserId,
        string FirstName,
        string LastName,
        string TenantId,
        string Role
    );

    private sealed record AntiforgeryTokenResponse(string Token);
}
