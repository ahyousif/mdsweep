using Mdsweep.Api.Configuration;
using Mdsweep.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Mdsweep.Api.IntegrationTests;

public sealed class OidcOptionsTests
{
    [Fact]
    public void Default_signed_out_callback_is_the_oidc_middleware_endpoint()
    {
        var options = new OpenIdConnectOptions();

        Assert.Equal("/signout-callback-oidc", options.SignedOutCallbackPath);
    }

    [Fact]
    public async Task Sign_out_identifies_the_client_without_replaying_an_expired_id_token()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IOptions<KeycloakAuthenticationOptions>>(
            Options.Create(
                new KeycloakAuthenticationOptions
                {
                    Authority = "https://keycloak.test/realms/mdsweep",
                    ClientId = "mdsweep-test",
                    ClientSecret = "test-secret",
                }
            )
        );
        builder.AddApi();
        await using var services = builder.Services.BuildServiceProvider();
        var options = services
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);
        var context = new RedirectContext(
            new DefaultHttpContext { RequestServices = services },
            new AuthenticationScheme(
                OpenIdConnectDefaults.AuthenticationScheme,
                displayName: null,
                typeof(OpenIdConnectHandler)
            ),
            options,
            new AuthenticationProperties()
        )
        {
            ProtocolMessage = new OpenIdConnectMessage
            {
                IdTokenHint = "expired-id-token",
                PostLogoutRedirectUri = "https://web.mdsweep.test/signout-callback-oidc",
            },
        };

        await options.Events.OnRedirectToIdentityProviderForSignOut(context);

        Assert.Null(context.ProtocolMessage.IdTokenHint);
        Assert.Equal("mdsweep-test", context.ProtocolMessage.ClientId);
    }
}
