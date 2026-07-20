using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace QueryPlus.Web.Auth;

/// <summary>
/// Ensures browser redirects never use Docker-internal Keycloak hostnames.
/// </summary>
public static class KeycloakUrlRewriter
{
    public static void RewriteBrowserFacingIssuer(
        OpenIdConnectMessage message,
        string publicAuthority)
    {
        if (!Uri.TryCreate(publicAuthority, UriKind.Absolute, out var publicAuthorityUri))
        {
            return;
        }

        message.IssuerAddress = Rewrite(message.IssuerAddress, publicAuthorityUri)
                                ?? message.IssuerAddress;
    }

    private static string? Rewrite(string? url, Uri publicAuthorityUri)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        // Docker-internal service name must never appear in browser redirects.
        if (!uri.Host.Equals("keycloak", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = publicAuthorityUri.Scheme,
            Host = publicAuthorityUri.Host,
            Port = publicAuthorityUri.IsDefaultPort ? -1 : publicAuthorityUri.Port
        };
        return builder.Uri.ToString();
    }
}
