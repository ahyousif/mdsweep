using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Mdsweep.Api.IntegrationTests;

public sealed class OidcOptionsTests
{
    [Fact]
    public void Default_signed_out_callback_is_the_oidc_middleware_endpoint()
    {
        var options = new OpenIdConnectOptions();

        Assert.Equal("/signout-callback-oidc", options.SignedOutCallbackPath);
    }
}
