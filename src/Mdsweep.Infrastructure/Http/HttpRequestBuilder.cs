namespace Mdsweep.Infrastructure.Http;

public sealed class HttpRequestBuilder(IHttpClientFactory httpClientFactory, string clientName)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly string _clientName = clientName;
    private HttpContent? _content;
    private HttpMethod _method = HttpMethod.Get;
    private string _url = string.Empty;
    private readonly List<KeyValuePair<string, string>> _headers = [];
    private bool _successRequired = true;

    public HttpRequestBuilder WithBody<T>(T body, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(body, options);
        _content = new StringContent(json, Encoding.UTF8, "application/json");
        return this;
    }

    public HttpRequestBuilder WithBearerToken(string token)
    {
        _headers.Add(new("Authorization", $"Bearer {token}"));
        return this;
    }

    public HttpRequestBuilder WithContent(HttpContent content)
    {
        _content = content;
        return this;
    }

    public HttpRequestBuilder WithMethod(HttpMethod method)
    {
        _method = method;
        return this;
    }

    public HttpRequestBuilder WithHeader(string key, string value)
    {
        _headers.Add(new(key, value));
        return this;
    }

    public HttpRequestBuilder WithHeaders(IEnumerable<KeyValuePair<string, string>> headers)
    {
        _headers.AddRange(headers);
        return this;
    }

    public HttpRequestBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }

    public HttpRequestBuilder WithSuccessRequired(bool required = true)
    {
        _successRequired = required;
        return this;
    }

    public async Task<HttpResponseMessage> SendAsync(CancellationToken ct = default)
    {
        Guard.Against.NullOrWhiteSpace(_url, nameof(_url), "Request URL must be provided.");

        using var request = new HttpRequestMessage(_method, _url) { Content = _content };

        foreach (var header in _headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        var client = _httpClientFactory.CreateClient(_clientName);
        var response = await client.SendAsync(request, ct);

        if (_successRequired)
            response.EnsureSuccessStatusCode();

        return response;
    }
}
