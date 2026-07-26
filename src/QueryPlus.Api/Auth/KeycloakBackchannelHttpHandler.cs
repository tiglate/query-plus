namespace QueryPlus.Api.Auth;

public sealed class KeycloakBackchannelHttpHandler(
    string publicHost,
    int publicPort,
    string internalHost,
    int internalPort) : DelegatingHandler(new HttpClientHandler())
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is { } uri && uri.Host.Equals(publicHost, StringComparison.OrdinalIgnoreCase) &&
            (uri.Port == publicPort || uri.IsDefaultPort && publicPort is 80 or 443))
        {
            request.RequestUri = new UriBuilder(uri) { Host = internalHost, Port = internalPort }.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
