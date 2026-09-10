using System.Net;
using System.Net.Http.Json;
using Mdsweep.Application.Users;
using Mdsweep.Infrastructure.Identity;

namespace Mdsweep.Api.IntegrationTests;

public sealed class KeycloakUserAdministrationTests
{
    [Fact]
    public async Task Resend_works_after_Keycloak_signup_but_before_local_acceptance()
    {
        var resent = false;
        using var client = new HttpClient(
            new StubHandler(request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                if (path.EndsWith("/token"))
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = JsonContent.Create(new { access_token = "synthetic-token" }),
                        }
                    );
                if (path.EndsWith("/invite-user"))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict));
                if (path.EndsWith("/users"))
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = JsonContent.Create(
                                new[] { new { id = "synthetic-subject", email = "driver@example.test" } }
                            ),
                        }
                    );
                Assert.EndsWith("/members/invite-existing-user", path);
                resent = true;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            })
        );
        await Create(client)
            .InviteAsync(
                "synthetic-organization",
                "driver@example.test",
                "Synthetic",
                "Driver",
                "synthetic-invitation-token",
                default
            );
        Assert.True(resent);
    }

    [Fact]
    public async Task Uses_native_organization_signup_and_password_reset_email_contracts()
    {
        var requests = new List<(string Method, string Path, string Body)>();
        using var client = new HttpClient(
            new StubHandler(async request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/token"))
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new { access_token = "synthetic-token" }),
                    };
                Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
                requests.Add(
                    (
                        request.Method.Method,
                        request.RequestUri.AbsolutePath,
                        request.Content is null ? "" : await request.Content.ReadAsStringAsync()
                    )
                );
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            })
        );
        var identity = Create(client);
        await identity.InviteAsync(
            "synthetic-organization",
            "driver@example.test",
            "Synthetic",
            "Driver",
            "synthetic-invitation-token",
            default
        );
        await identity.SendPasswordResetAsync("synthetic-subject", default);
        Assert.Equal(
            "/admin/realms/mdsweep/organizations/synthetic-organization/members/invite-user",
            requests[0].Path
        );
        Assert.Contains("email=driver%40example.test", requests[0].Body);
        Assert.Equal("PUT", requests[1].Method);
        Assert.Equal("/admin/realms/mdsweep/users/synthetic-subject/execute-actions-email", requests[1].Path);
        Assert.Equal("[\"UPDATE_PASSWORD\"]", requests[1].Body);
    }

    [Fact]
    public async Task Delivery_failure_is_actionable_without_exposing_the_upstream_response()
    {
        using var client = new HttpClient(
            new StubHandler(request =>
                Task.FromResult(
                    request.RequestUri!.AbsolutePath.EndsWith("/token")
                        ? new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = JsonContent.Create(new { access_token = "synthetic-token" }),
                        }
                        : new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent("private upstream diagnostic"),
                        }
                )
            )
        );
        var exception = await Assert.ThrowsAsync<IdentityAdministrationException>(() =>
            Create(client)
                .InviteAsync(
                    "synthetic-organization",
                    "driver@example.test",
                    "Synthetic",
                    "Driver",
                    "synthetic-invitation-token",
                    default
                )
        );
        Assert.Contains("email delivery", exception.Message);
        Assert.DoesNotContain("private upstream diagnostic", exception.Message);
    }

    private static KeycloakUserAdministration Create(HttpClient client) =>
        new(
            client,
            Options.Create(
                new KeycloakAdministrationOptions { ClientId = "admin-client", ClientSecret = "synthetic-secret" }
            ),
            Options.Create(
                new KeycloakAuthenticationOptions
                {
                    Authority = "https://identity.example.test/realms/mdsweep",
                    ClientId = "server",
                    ClientSecret = "synthetic-secret",
                }
            )
        );

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => response(request);
    }
}
