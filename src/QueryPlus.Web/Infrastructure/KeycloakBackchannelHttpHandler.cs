namespace QueryPlus.Web.Infrastructure;

/// <summary>
/// Rewrites browser-facing Keycloak hosts (e.g. localhost:8080) to the Docker-internal
/// hostname (keycloak:8080) for server-to-server OIDC calls from inside a container.
/// </summary>
public sealed class KeycloakBackchannelHttpHandler : DelegatingHandler
{
    private readonly string _publicHost;
    private readonly int _publicPort;
    private readonly string _internalHost;
    private readonly int _internalPort;

    public KeycloakBackchannelHttpHandler(
        string publicHost,
        int publicPort,
        string internalHost,
        int internalPort)
        : base(new HttpClientHandler())
    {
        _publicHost = publicHost;
        _publicPort = publicPort;
        _internalHost = internalHost;
        _internalPort = internalPort;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is { } uri
            && uri.Host.Equals(_publicHost, StringComparison.OrdinalIgnoreCase)
            && (uri.Port == _publicPort || (uri.IsDefaultPort && _publicPort is 80 or 443)))
        {
            var builder = new UriBuilder(uri)
            {
                Host = _internalHost,
                Port = _internalPort
            };
            request.RequestUri = builder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
