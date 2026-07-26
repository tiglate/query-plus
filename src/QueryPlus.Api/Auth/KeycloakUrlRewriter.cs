using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace QueryPlus.Api.Auth;

public static class KeycloakUrlRewriter
{
    public static void RewriteBrowserFacingIssuer(OpenIdConnectMessage message, string publicAuthority)
    {
        if (!Uri.TryCreate(publicAuthority, UriKind.Absolute, out var authority) ||
            string.IsNullOrWhiteSpace(message.IssuerAddress) ||
            !Uri.TryCreate(message.IssuerAddress, UriKind.Absolute, out var issuer)) return;
        if (!issuer.Host.Equals("keycloak", StringComparison.OrdinalIgnoreCase) &&
            !issuer.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)) return;
        message.IssuerAddress = new UriBuilder(issuer)
            {
                Scheme = authority.Scheme, Host = authority.Host, Port = authority.IsDefaultPort ? -1 : authority.Port
            }
            .Uri.ToString();
    }
}
