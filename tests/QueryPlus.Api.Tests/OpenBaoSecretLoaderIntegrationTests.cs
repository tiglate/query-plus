using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using QueryPlus.Hosting;

namespace QueryPlus.Api.Tests;

/// <summary>
/// FetchSecretsAsync talks to a real OpenBao KV v2 API over HTTP - no mockable seam exists (nor
/// should one; it's a thin, three-line HTTP client wrapper) - so this is integration-only,
/// mirroring how DapperStoredProcedureExecutor is only exercised against a real SQL Server in
/// QueryPlus.Integration.Tests. Uses Testcontainers' generic container support directly since
/// there's no dedicated OpenBao module.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OpenBaoSecretLoaderIntegrationTests : IAsyncLifetime
{
    private const string RootToken = "test-root-token";
    private IContainer _container = null!;

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("openbao/openbao:2.6.1")
            .WithEnvironment("BAO_DEV_ROOT_TOKEN_ID", RootToken)
            .WithPortBinding(8200, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(8200).ForPath("/v1/sys/health")))
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private string Address => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(8200)}";

    [Fact]
    public async Task FetchSecretsAsync_RoundTripsAValueWrittenViaTheRestApi()
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("X-Vault-Token", RootToken);
        var response = await httpClient.PostAsync(
            $"{Address}/v1/secret/data/queryplus",
            new StringContent("""{"data":{"ConnectionStrings__DefaultConnection":"Server=test;","Keycloak__ClientSecret":"s3cr3t"}}"""));
        response.EnsureSuccessStatusCode();

        var secrets = await OpenBaoSecretLoader.FetchSecretsAsync(Address, RootToken, "secret", "queryplus");

        secrets.Should().Contain(new KeyValuePair<string, string>(
            "ConnectionStrings__DefaultConnection", "Server=test;"));
        secrets.Should().Contain(new KeyValuePair<string, string>("Keycloak__ClientSecret", "s3cr3t"));
    }

    [Fact]
    public async Task FetchSecretsAsync_MissingSecretPath_Throws()
    {
        var act = async () =>
            await OpenBaoSecretLoader.FetchSecretsAsync(Address, RootToken, "secret", "does-not-exist");

        await act.Should().ThrowAsync<Exception>();
    }
}
