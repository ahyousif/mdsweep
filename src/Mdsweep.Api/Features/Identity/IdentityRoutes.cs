namespace Mdsweep.Api.Features.Identity;

internal static class IdentityRoutes
{
    public const string Login = "/api/auth/login";
    public const string CurrentUser = "/api/auth/me";
    public const string ProviderContext = "/api/auth/provider-context";
    public const string Antiforgery = "/api/auth/antiforgery";
    public const string Logout = "/api/auth/logout";
}
