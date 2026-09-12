using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Azure.Core;

namespace Mdsweep.Utility.Database;

public sealed class AzurePostgresApplicationDatabaseManager(
    HttpClient client,
    TokenCredential credential,
    string subscriptionId,
    string resourceGroup,
    string serverName
) : IApplicationDatabaseManager
{
    private const string DatabaseName = "mdsweep";
    private const string ApiVersion = "2025-08-01";

    public async Task EnsureExistsAsync(CancellationToken cancellationToken = default)
    {
        using var get = await SendAsync(HttpMethod.Get, cancellationToken);
        if (get.IsSuccessStatusCode)
        {
            return;
        }

        if (get.StatusCode is not HttpStatusCode.NotFound)
        {
            await ThrowForFailureAsync(get, cancellationToken);
        }

        using var put = await SendAsync(
            HttpMethod.Put,
            cancellationToken,
            new StringContent("{\"properties\":{}}", Encoding.UTF8, "application/json")
        );
        await ThrowForFailureAsync(put, cancellationToken);
        await WaitForStateAsync(shouldExist: true, cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return;
        }

        await ThrowForFailureAsync(response, cancellationToken);
        await WaitForStateAsync(shouldExist: false, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        CancellationToken cancellationToken,
        HttpContent? content = null
    )
    {
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(["https://management.azure.com/.default"]),
            cancellationToken
        );
        var request = new HttpRequestMessage(method, ResourceUri) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await client.SendAsync(request, cancellationToken);
    }

    private string ResourceUri =>
        $"https://management.azure.com/subscriptions/{Uri.EscapeDataString(subscriptionId)}"
        + $"/resourceGroups/{Uri.EscapeDataString(resourceGroup)}"
        + "/providers/Microsoft.DBforPostgreSQL"
        + $"/flexibleServers/{Uri.EscapeDataString(serverName)}"
        + $"/databases/{DatabaseName}?api-version={ApiVersion}";

    private async Task WaitForStateAsync(bool shouldExist, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            using var response = await SendAsync(HttpMethod.Get, cancellationToken);
            var exists = response.IsSuccessStatusCode;
            if (exists == shouldExist)
            {
                return;
            }

            if (response.StatusCode is not HttpStatusCode.NotFound)
            {
                await ThrowForFailureAsync(response, cancellationToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new TimeoutException("Azure PostgreSQL did not reach the requested database state within two minutes.");
    }

    private static async Task ThrowForFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var details = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Azure PostgreSQL database operation failed with status {(int)response.StatusCode}: {details}",
            null,
            response.StatusCode
        );
    }
}
