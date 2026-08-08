using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using QueryPlus.Api.Tests.Infrastructure;

namespace QueryPlus.Api.Tests.Integration;

/// <summary>
/// GET /login (Program.cs) is the only caller of the local `IsLocalUrl` guard - a `static` local
/// function with no unit-test seam of its own - so this open-redirect protection is only
/// reachable through this HTTP-level test. QueryPlusApiApplicationFactory keeps the real OpenIdConnect
/// scheme registered and stubs its ConfigurationManager (see StaticConfigurationManager in the
/// factory), so the Results.Challenge(...) call here produces a real redirect to the stubbed
/// Keycloak authorization endpoint without needing a live Keycloak.
/// </summary>
public sealed class LoginRedirectTests(QueryPlusApiApplicationFactory factory)
    : IClassFixture<QueryPlusApiApplicationFactory>
{
    private const string AuthorizationEndpoint = "http://localhost:8080/realms/queryplus/protocol/openid-connect/auth";

    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Login_WithNoReturnUrl_RedirectsToTheStubbedAuthorizationEndpoint()
    {
        var response = await _client.GetAsync("/login");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.Should().StartWith(AuthorizationEndpoint);
        location.Should().Contain("client_id=test-client");
        location.Should().Contain("response_type=code");
        location.Should().Contain("scope=");
        location.Should().Contain("state=");
    }

    [Fact]
    public async Task Login_WithLegitimateLocalReturnUrl_StillRedirectsToTheAuthorizationEndpoint()
    {
        var response = await _client.GetAsync("/login?returnUrl=%2Fadmin%2Fcategories");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().StartWith(AuthorizationEndpoint);
    }

    [Theory]
    [InlineData("//evil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("https://evil.com")]
    [InlineData("http://evil.com/path")]
    public async Task Login_WithAnOpenRedirectAttemptInReturnUrl_NeverLeaksTheAttackerHost(string maliciousReturnUrl)
    {
        var response = await _client.GetAsync($"/login?returnUrl={Uri.EscapeDataString(maliciousReturnUrl)}");

        // IsLocalUrl rejects all four shapes above, so the challenge must still target the real
        // Keycloak host - never redirect (directly or via the protected state/cookie) toward the
        // attacker-supplied host.
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.Should().StartWith(AuthorizationEndpoint);
        location.Should().NotContain("evil.com");

        foreach (var setCookie in response.Headers.TryGetValues("Set-Cookie", out var cookies)
                     ? cookies
                     : [])
        {
            setCookie.Should().NotContain("evil.com");
        }
    }
}
