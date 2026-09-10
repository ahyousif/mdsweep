namespace Mdsweep.Api.Features.Identity;

internal static class AuthenticationEndpoints
{
    public static RouteGroupBuilder MapAuthenticationEndpoints(this RouteGroupBuilder auth)
    {
        auth.MapGet("/login", Login).AllowAnonymous();

        auth.MapGet("/register", Register).AllowAnonymous();

        auth.MapPost("/logout", Logout);

        return auth;
    }

    private static IResult Login(string? returnUrl)
    {
        return Challenge(returnUrl);
    }

    private static IResult Register(string? returnUrl)
    {
        return Challenge(returnUrl, registration: true);
    }

    private static IResult Challenge(string? returnUrl, bool registration = false)
    {
        var properties = new AuthenticationProperties { RedirectUri = SafeReturnPath(returnUrl) };

        if (registration)
        {
            properties.Items["register"] = "true";
        }

        return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
    }

    private static IResult Logout()
    {
        return Results.SignOut(
            properties: null,
            authenticationSchemes:
            [
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme,
            ]
        );
    }

    private static string SafeReturnPath(string? returnUrl)
    {
        if (
            string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith('/')
            || returnUrl.StartsWith("//")
            || returnUrl.StartsWith("/\\")
        )
        {
            return "/";
        }

        return returnUrl;
    }
}
