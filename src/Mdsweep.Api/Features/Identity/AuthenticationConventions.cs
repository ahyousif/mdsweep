namespace Mdsweep.Api.Features.Identity;

public static class AuthenticationConventions
{
    public const string CookieName = ".Mdsweep.Auth";
    public const string AntiforgeryCookieName = ".Mdsweep.Antiforgery";
    public const string AntiforgeryHeaderName = "X-XSRF-TOKEN";
}
