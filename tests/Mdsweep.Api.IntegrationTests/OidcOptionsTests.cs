using Mdsweep.Api.Configuration;
using Mdsweep.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
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
    public async Task Sign_out_preserves_the_saved_id_token_hint()
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
                IdTokenHint = "saved-id-token",
                PostLogoutRedirectUri = "https://web.mdsweep.test/signout-callback-oidc",
            },
        };

        await options.Events.OnRedirectToIdentityProviderForSignOut(context);

        Assert.Equal("saved-id-token", context.ProtocolMessage.IdTokenHint);
    }
}
