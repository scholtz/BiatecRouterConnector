namespace BiatecRouterConnector;

/// <summary>Configuration used by <see cref="BiatecRouterClient"/>.</summary>
public sealed class BiatecRouterClientOptions
{
    /// <summary>Base address of the Biatec Router API. Defaults to the public production endpoint.</summary>
    public Uri BaseUri { get; init; } = new("https://router.api.biatec.io");

    /// <summary>
    /// Base64-encoded, msgpack-serialized ARC-0014 authentication transaction sent as the
    /// <c>Authorization</c> header, or <see langword="null"/> to make unauthenticated requests.
    /// </summary>
    public string? Authorization { get; init; }
}
