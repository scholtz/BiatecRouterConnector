using System.Net.Http.Headers;

namespace BiatecRouterConnector;

internal sealed class AuthorizationHeaderHandler : DelegatingHandler
{
    private readonly Func<string?> _getAuthorizationValue;

    public AuthorizationHeaderHandler(Func<string?> getAuthorizationValue)
        => _getAuthorizationValue = getAuthorizationValue;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var value = _getAuthorizationValue();
        if (!string.IsNullOrWhiteSpace(value))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(value);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
