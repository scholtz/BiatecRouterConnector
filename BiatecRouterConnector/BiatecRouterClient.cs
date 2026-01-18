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
        Authorization = authorization;

        var baseUrl = (httpClient.BaseAddress ?? new Uri("https://algorand-trades.de-4.biatec.io")).ToString().TrimEnd('/');
        _api = new BiatecRouterApiClient(baseUrl, httpClient);
    }

    public HttpClient HttpClient { get; }

    public string? Authorization { get; set; }

    public BiatecRouterApiClient Api => _api;

    public static BiatecRouterClient Create(BiatecRouterClientOptions? options = null)
    {
        options ??= new BiatecRouterClientOptions();

        var authValue = options.Authorization;
        var handler = new AuthorizationHeaderHandler(() => authValue)
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = options.BaseUri
        };

        var client = new BiatecRouterClient(httpClient, (string?)null)
        {
            Authorization = authValue
        };

        return client;
    }
}
