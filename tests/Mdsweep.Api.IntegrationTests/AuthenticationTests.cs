using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Mdsweep.Api.IntegrationTests;

public sealed class AuthenticationTests : MdsweepIntegrationTest
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IStartupFilter, CookieSignInStartupFilter>();
    }

    [Fact]
    public async Task Logout_clears_the_cookie_and_uses_the_saved_id_token_hint()
    {
        var cookieOptions = Application.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        using var client = Application.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true }
        );
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/test/sign-in")).StatusCode);
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsync(
            "/api/auth/logout?returnUrl=%2Finvitations%2Faccept%3Ftoken%3DABC",
            content: null
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = Assert.IsType<Uri>(response.Headers.Location);
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.True(
            query.TryGetValue(OpenIdConnectParameterNames.IdTokenHint, out var idTokenHint),
            $"Logout redirect did not contain id_token_hint: {location}"
        );
        Assert.Equal("saved-id-token", idTokenHint);
        var oidcOptions = Application.Services
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);
        var signOutProperties = oidcOptions.StateDataFormat.Unprotect(query[OpenIdConnectParameterNames.State]!);
        Assert.Equal("/invitations/accept?token=ABC", signOutProperties!.RedirectUri);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith($"{cookieOptions.Cookie.Name}=;")
        );
    }

    private sealed class CookieSignInStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, following) =>
            {
                if (context.Request.Path != "/test/sign-in")
                {
                    await following();
                    return;
                }

                var properties = new AuthenticationProperties();
                properties.StoreTokens(
                    [
                        new AuthenticationToken
                        {
                            Name = OpenIdConnectParameterNames.IdToken,
                            Value = "saved-id-token",
                        },
                    ]
                );
                var principal = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [new Claim("sub", "logout-user")],
                        CookieAuthenticationDefaults.AuthenticationScheme
                    )
                );
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    properties
                );
                context.Response.StatusCode = StatusCodes.Status204NoContent;
            });
            next(app);
        };
    }
}
