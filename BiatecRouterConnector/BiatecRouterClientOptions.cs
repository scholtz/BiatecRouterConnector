namespace BiatecRouterConnector;

public sealed class BiatecRouterClientOptions
{
    public Uri BaseUri { get; init; } = new("https://algorand-trades.de-4.biatec.io");

    public string? Authorization { get; init; }
}
