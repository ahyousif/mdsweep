using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Identity;

public static class LoginEndpoint
{
    [AllowAnonymous]
    [WolverineGet(IdentityRoutes.Login)]
    public static IResult Get() =>
        Results.Challenge(
            new AuthenticationProperties { RedirectUri = "/" },
            [OpenIdConnectDefaults.AuthenticationScheme]
        );
}
