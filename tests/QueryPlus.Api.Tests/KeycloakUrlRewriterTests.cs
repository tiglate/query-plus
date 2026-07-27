using FluentAssertions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QueryPlus.Api.Auth;

namespace QueryPlus.Api.Tests;

public class KeycloakUrlRewriterTests
{
    [Fact]
    public void RewriteBrowserFacingIssuer_RewritesInternalDockerHostname_ToPublicAuthority()
    {
        var msg = new OpenIdConnectMessage
        {
            IssuerAddress = "http://keycloak:8080/realms/queryplus/protocol/openid-connect/auth"
        };
        var publicAuthority = "http://localhost:8080/realms/queryplus";

        KeycloakUrlRewriter.RewriteBrowserFacingIssuer(msg, publicAuthority);

        msg.IssuerAddress.Should().Be("http://localhost:8080/realms/queryplus/protocol/openid-connect/auth");
    }

    [Fact]
    public void RewriteBrowserFacingIssuer_LeavesExternalHostUnchanged()
    {
        var msg = new OpenIdConnectMessage
        {
            IssuerAddress = "http://idp.example.com/auth"
        };
        var publicAuthority = "http://localhost:8080/realms/queryplus";

        KeycloakUrlRewriter.RewriteBrowserFacingIssuer(msg, publicAuthority);

        msg.IssuerAddress.Should().Be("http://idp.example.com/auth");
    }
}
