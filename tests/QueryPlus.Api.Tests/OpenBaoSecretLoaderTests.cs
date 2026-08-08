using FluentAssertions;
using QueryPlus.Api.Hosting;

namespace QueryPlus.Api.Tests;

public sealed class OpenBaoSecretLoaderTests : IDisposable
{
    private readonly string? _originalAddr = Environment.GetEnvironmentVariable("OPENBAO_ADDR");
    private readonly string? _originalToken = Environment.GetEnvironmentVariable("OPENBAO_TOKEN");

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("OPENBAO_ADDR", _originalAddr);
        Environment.SetEnvironmentVariable("OPENBAO_TOKEN", _originalToken);
    }

    [Fact]
    public async Task LoadFromEnvironmentAsync_NoOps_WhenBothAddressAndTokenAreUnset()
    {
        Environment.SetEnvironmentVariable("OPENBAO_ADDR", null);
        Environment.SetEnvironmentVariable("OPENBAO_TOKEN", null);

        // Must return without ever attempting a network call - an unreachable OpenBao is only
        // fatal once explicitly configured, never by default (keeps the fast test suite/CI
        // network-free even if a developer happens to have these vars set in their shell).
        var act = async () => await OpenBaoSecretLoader.LoadFromEnvironmentAsync();

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("http://127.0.0.1:8200", null)]
    [InlineData(null, "some-token")]
    public async Task LoadFromEnvironmentAsync_NoOps_WhenOnlyOneOfAddressOrTokenIsSet(
        string? address, string? token)
    {
        Environment.SetEnvironmentVariable("OPENBAO_ADDR", address);
        Environment.SetEnvironmentVariable("OPENBAO_TOKEN", token);

        var act = async () => await OpenBaoSecretLoader.LoadFromEnvironmentAsync();

        await act.Should().NotThrowAsync();
    }
}
