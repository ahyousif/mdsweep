using Microsoft.AspNetCore.Antiforgery;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Identity;

public static class GetAntiforgeryTokenEndpoint
{
    [WolverineGet(IdentityRoutes.Antiforgery)]
    public static IResult Get(IAntiforgery antiforgery, HttpContext httpContext)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        httpContext.Response.Cookies.Append(
            "XSRF-TOKEN",
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
}
