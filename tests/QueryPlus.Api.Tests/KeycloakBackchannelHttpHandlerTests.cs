using FluentAssertions;
using QueryPlus.Api.Auth;

namespace QueryPlus.Api.Tests;

/// <summary>
/// KeycloakBackchannelHttpHandler hardcodes its inner handler (`: base(new HttpClientHandler())`),
/// so there is no seam to inject a fake transport. Instead these tests point the rewritten target
/// at loopback ports nothing listens on (matching the "deliberately unreachable" trick already used
/// by QueryPlusApiApplicationFactory's connection string) - the real HttpClientHandler fails fast
/// with a connection-refused error, and by the time it does, SendAsync has already mutated
/// request.RequestUri in place, which is what these tests assert on.
/// </summary>
public class KeycloakBackchannelHttpHandlerTests
{
    [Fact]
    public async Task SendAsync_RewritesHostAndPort_WhenRequestMatchesPublicHostAndPort()
    {
        var handler = new KeycloakBackchannelHttpHandler("127.0.0.1", 9999, "127.0.0.1", 1);
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:9999/realms/queryplus/auth");

        var act = async () => await invoker.SendAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        request.RequestUri!.Host.Should().Be("127.0.0.1");
        request.RequestUri.Port.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_RewritesDefaultPort_WhenPublicPortIsTheImplicitDefault()
    {
        var handler = new KeycloakBackchannelHttpHandler("127.0.0.1", 80, "127.0.0.1", 1);
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1/realms/queryplus/auth"); // implicit :80

        var act = async () => await invoker.SendAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        request.RequestUri!.Host.Should().Be("127.0.0.1");
        request.RequestUri.Port.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_LeavesRequestUnchanged_WhenPortDoesNotMatchPublicPort()
    {
        var handler = new KeycloakBackchannelHttpHandler("127.0.0.1", 9999, "127.0.0.1", 1);
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:12345/realms/queryplus/auth");

        var act = async () => await invoker.SendAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        request.RequestUri!.Port.Should().Be(12345); // unchanged - never rewritten to the internal port
    }
}
