using System.Security.Claims;
using System.Text.Encodings.Web;
using Mdsweep.Api.Common.Authentication;

namespace Mdsweep.Api.IntegrationTests;

public sealed class DispatcherAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers["X-Test-Anonymous"] == "true")
            return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new List<Claim>
        {
            new("sub", Request.Headers["X-Test-Subject"].FirstOrDefault() ?? "dispatcher-test"),
            new("email", Request.Headers["X-Test-Email"].FirstOrDefault() ?? "dispatcher@example.test"),
            new(
                "email_verified",
                Request.Headers["X-Test-Email-Verified"].FirstOrDefault() ?? bool.TrueString
            ),
        };
        var tenantId =
            Request.Headers["X-Test-Tenant"].FirstOrDefault()
            ?? "mdsw-eep2-3456";
        if (!string.IsNullOrWhiteSpace(tenantId))
            claims.Add(new Claim(CustomClaimTypes.ActiveTenantId, tenantId));
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name))
        );
    }
}
