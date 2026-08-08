using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Application.DependencyInjection;
using QueryPlus.Data.DependencyInjection;
using QueryPlus.Data.Seed;

namespace QueryPlus.Integration.Tests;

/// <summary>
/// Base for every real-SQL-Server integration test class. Carves out a dedicated database on the
/// shared <see cref="SqlServerContainerFixture"/> server, wires the exact same DI graph the app
/// uses in production (<c>AddApplication</c> + <c>AddData</c> - no test doubles), applies
/// migrations, and installs the demo catalog/SQL objects so tests have real fixtures to work
/// against (e.g. <c>dbo.Sp_Demo_Numbers_Paged</c>).
///
/// xUnit constructs a fresh instance of the test class - and therefore runs
/// <see cref="InitializeAsync"/>/<see cref="DisposeAsync"/> - once per [Fact], so every test gets
/// its own database. Tests within one class never share state; no Respawn-style reset is needed.
/// </summary>
[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;
    private ServiceProvider? _provider;

    protected IntegrationTestBase(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    protected string DatabaseName { get; } = $"qpit_{Guid.NewGuid():N}";

    protected IServiceProvider Services =>
        _provider ?? throw new InvalidOperationException("Fixture not initialized yet.");

    public async Task InitializeAsync()
    {
        await using (var master = new SqlConnection(_fixture.MasterConnectionString))
        {
            await master.OpenAsync();
            await using var create = master.CreateCommand();
            create.CommandText = $"CREATE DATABASE [{DatabaseName}];";
            await create.ExecuteNonQueryAsync();
        }

        var scopedConnectionString = new SqlConnectionStringBuilder(_fixture.MasterConnectionString)
        {
            InitialCatalog = DatabaseName,
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = scopedConnectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication();
        services.AddData(configuration);
        _provider = services.BuildServiceProvider();

        await using var scope = _provider.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }

        await using var master = new SqlConnection(_fixture.MasterConnectionString);
        await master.OpenAsync();
        await using var drop = master.CreateCommand();
        // Force off any pooled connections left open by the app's own connection pool before
        // dropping, or SQL Server refuses with "database is in use".
        drop.CommandText = $"""
            ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{DatabaseName}];
            """;
        await drop.ExecuteNonQueryAsync();
    }

    protected async Task<TResult> WithScopeAsync<TResult>(Func<IServiceProvider, Task<TResult>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    protected async Task WithScopeAsync(Func<IServiceProvider, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider);
    }
}
