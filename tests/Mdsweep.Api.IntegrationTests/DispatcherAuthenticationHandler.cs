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
        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "dispatcher-test"),
                new Claim(CustomClaimTypes.ActiveTenantId, "mdsw-eep2-3456"),
            ],
            Scheme.Name
        );
        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name))
        );
    }
}
