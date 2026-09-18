using System.Net;
using System.Text;
using Mdsweep.Utility.DemoReset;
using Microsoft.Extensions.Options;

namespace Mdsweep.Api.IntegrationTests;

public sealed class KeycloakAdminClientTests
{
    [Fact]
    public async Task Recreates_realm_client_and_demo_administrator_with_the_returned_subject()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://keycloak.mdsweep.test/") };
        var client = new KeycloakAdminClient(
            httpClient,
            Options.Create(
                new KeycloakAdministrationOptions
                {
                    BaseUrl = "https://keycloak.mdsweep.test",
                    AutomationClientId = "mdsweep-demo-automation",
                    AutomationClientSecret = "synthetic-automation-secret",
                    OidcClientSecret = "synthetic-oidc-secret",
                    DemoAdminPassword = "synthetic-demo-password",
                }
            )
        );

        var userId = await client.RecreateDemoRealmAsync("https://app.mdsweep.test");

        Assert.Equal("new-demo-user-id", userId);
        Assert.Collection(
            handler.Requests,
            request => Assert.Equal("realms/master/protocol/openid-connect/token", request.Path),
            request => Assert.Equal("admin/realms/mdsweep", request.Path),
            request => Assert.Equal("admin/realms", request.Path),
            request =>
            {
                Assert.Equal("admin/realms/mdsweep/clients", request.Path);
                Assert.Contains("mdsweep-api", request.Body);
                Assert.Contains("synthetic-oidc-secret", request.Body);
                Assert.Contains("https://app.mdsweep.test/signin-oidc", request.Body);
            },
            request =>
            {
                Assert.Equal("admin/realms/mdsweep/users", request.Path);
                Assert.Contains("developer@mdsweep.com", request.Body);
            }
        );
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(
                new RecordedRequest(
                    request.Method,
                    request.RequestUri!.PathAndQuery.TrimStart('/'),
                    request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)
                )
            );

            return Requests.Count switch
            {
                1 => Json("{\"access_token\":\"synthetic-token\"}"),
                2 => new HttpResponseMessage(HttpStatusCode.NotFound),
                3 or 4 => new HttpResponseMessage(HttpStatusCode.Created),
                5 => new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Headers =
                    {
                        Location = new Uri("https://keycloak.mdsweep.test/admin/realms/mdsweep/users/new-demo-user-id"),
                    },
                },
                _ => throw new InvalidOperationException("Unexpected Keycloak request."),
            };
        }

        private static HttpResponseMessage Json(string content) =>
            new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path, string Body);
}
