namespace Mdsweep.Api.Common.Authentication;

public static class AuthenticationConventions
{
    public const string CookieName = ".Mdsweep.Auth";
    public const string AntiforgeryCookieName = ".Mdsweep.Antiforgery";
    public const string AntiforgeryHeaderName = "X-XSRF-TOKEN";
    public const string AntiforgeryRequestCookieName = "XSRF-TOKEN";
}
