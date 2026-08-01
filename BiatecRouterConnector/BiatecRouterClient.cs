using BiatecRouterConnector.Generated;

namespace BiatecRouterConnector;

/// <summary>
/// Wraps the generated <see cref="BiatecRouterApiClient"/> with ARC-0014 (<c>SigTx</c>) authorization
/// handling and a sensible default base address (<c>https://router.api.biatec.io</c>).
/// </summary>
public sealed class BiatecRouterClient
{
    private readonly BiatecRouterApiClient _api;

    /// <summary>Creates a client configured from <paramref name="options"/>.</summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> used to call the router API.</param>
    /// <param name="options">Optional base URI and ARC-0014 authorization header value.</param>
    public BiatecRouterClient(HttpClient httpClient, BiatecRouterClientOptions? options = null)
        : this(httpClient, options?.Authorization)
    {
        if (options?.BaseUri is { } baseUri)
        {
            httpClient.BaseAddress ??= baseUri;
        }
    }

    /// <summary>Creates a client with an explicit ARC-0014 authorization header value.</summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> used to call the router API.</param>
    /// <param name="authorization">
    /// Base64-encoded, msgpack-serialized ARC-0014 authentication transaction, or <see langword="null"/>
    /// to skip setting the <c>Authorization</c> header.
    /// </param>
    public BiatecRouterClient(HttpClient httpClient, string? authorization)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        HttpClient = httpClient;

        if (!string.IsNullOrWhiteSpace(authorization))
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SigTx", authorization);
        }

        var baseUrl = (httpClient.BaseAddress ?? new Uri("https://router.api.biatec.io")).ToString().TrimEnd('/');
        _api = new BiatecRouterApiClient(httpClient) { BaseUrl = baseUrl };
    }

    /// <summary>The underlying <see cref="System.Net.Http.HttpClient"/> used for all API calls.</summary>
    public HttpClient HttpClient { get; }

    /// <summary>The generated Biatec Router REST client (quote, route, stats, snapshot, routeTxs).</summary>
    public BiatecRouterApiClient Api => _api;
}
