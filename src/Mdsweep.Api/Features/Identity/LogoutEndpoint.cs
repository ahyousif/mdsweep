using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Identity;

public static class LogoutEndpoint
{
    [WolverinePost(IdentityRoutes.Logout)]
    public static IResult Post() =>
        Results.SignOut(
            properties: null,
            authenticationSchemes:
            [
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme,
            ]
        );
}
