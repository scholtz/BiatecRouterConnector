namespace BiatecRouterConnector;

public sealed class BiatecRouterClientOptions
{
    public Uri BaseUri { get; init; } = new("https://router.api.biatec.io");

    public string? Authorization { get; init; }
}
