using Testcontainers.MsSql;

namespace QueryPlus.Integration.Tests;

/// <summary>
/// One SQL Server container for the entire "Integration" collection - started once and shared by
/// every test class in this project (via <see cref="IntegrationCollection"/>), since container
/// startup (image pull + health check) dominates runtime far more than any individual test.
/// Each test class carves out its own database on this shared server (see
/// <see cref="IntegrationTestBase"/>) so tests never see each other's data.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string MasterConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<SqlServerContainerFixture>
{
    public const string Name = "Integration";
}
