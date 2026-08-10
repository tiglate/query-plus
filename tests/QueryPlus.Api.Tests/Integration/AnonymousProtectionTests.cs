using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using QueryPlus.Api.Tests.Infrastructure;

namespace QueryPlus.Api.Tests.Integration;

public sealed class AnonymousProtectionTests(AnonymousQueryPlusApiApplicationFactory factory)
    : IClassFixture<AnonymousQueryPlusApiApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Anonymous_health_remains_anonymous()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Anonymous_categories_returns_401_under_fallback_policy()
    {
        var response = await _client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task Anonymous_procedures_returns_401_under_fallback_policy()
    {
        var response = await _client.GetAsync("/api/procedures");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task Anonymous_jobs_returns_401_under_fallback_policy()
    {
        var response = await _client.GetAsync("/api/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task Anonymous_auth_user_returns_200_with_unauthenticated_state()
    {
        var response = await _client.GetAsync("/api/auth/user");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Location.Should().BeNull();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"isAuthenticated\":false");
        json.Should().Contain("\"username\":\"anonymous\"");
    }
}
