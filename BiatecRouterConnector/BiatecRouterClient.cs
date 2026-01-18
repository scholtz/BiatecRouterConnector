using BiatecRouterConnector.Generated;

namespace BiatecRouterConnector;

public sealed class BiatecRouterClient
{
    private readonly BiatecRouterApiClient _api;

    public BiatecRouterClient(HttpClient httpClient, BiatecRouterClientOptions? options = null)
        : this(httpClient, options?.Authorization)
    {
        if (options?.BaseUri is { } baseUri)
        {
            httpClient.BaseAddress ??= baseUri;
        }
    }

    public BiatecRouterClient(HttpClient httpClient, string? authorization)
    {
        if (httpClient is null) throw new ArgumentNullException(nameof(httpClient));

        HttpClient = httpClient;

        if (!string.IsNullOrWhiteSpace(authorization))
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SigTx", authorization);
        }

        var baseUrl = (httpClient.BaseAddress ?? new Uri("https://router.api.biatec.io")).ToString().TrimEnd('/');
        _api = new BiatecRouterApiClient(baseUrl, httpClient);
    }

    public HttpClient HttpClient { get; }

    public BiatecRouterApiClient Api => _api;
}
