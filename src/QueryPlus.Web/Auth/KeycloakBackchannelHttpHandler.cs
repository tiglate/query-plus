namespace QueryPlus.Web.Auth;

/// <summary>
/// Rewrites browser-facing Keycloak hosts (e.g. localhost:8080) to the Docker-internal
/// hostname (keycloak:8080) for server-to-server OIDC calls from inside a container.
/// </summary>
public sealed class KeycloakBackchannelHttpHandler(
    string publicHost,
    int publicPort,
    string internalHost,
    int internalPort)
    : DelegatingHandler(new HttpClientHandler())
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is { } uri
            && uri.Host.Equals(publicHost, StringComparison.OrdinalIgnoreCase)
            && (uri.Port == publicPort || (uri.IsDefaultPort && publicPort is 80 or 443)))
        {
            var builder = new UriBuilder(uri)
            {
                Host = internalHost,
                Port = internalPort
            };
            request.RequestUri = builder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
