using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueryPlus.Application.DependencyInjection;
using QueryPlus.Application.Options;
using QueryPlus.Hosting;
using QueryPlus.Infrastructure.DependencyInjection;
using QueryPlus.SchedulerSync;

try
{
    EnvFileLoader.LoadFromAncestors(Directory.GetCurrentDirectory());
    EnvFileLoader.LoadFromAncestors(AppContext.BaseDirectory);
    await OpenBaoSecretLoader.LoadFromEnvironmentAsync();

    var mode = ParseMode(args);
    if (mode is null)
    {
        Console.Error.WriteLine(
            $"Unknown arguments: '{string.Join(' ', args)}'. Expected no arguments (defaults to --sync), --sync, or --watchdog.");
        return 1;
    }

    // systemd may invoke this with an arbitrary working directory, so the config base path is
    // the app's own output directory, never Directory.GetCurrentDirectory().
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddApplication();
    services.AddInfrastructure(configuration);
    services.AddOptions<JobsOptions>().Bind(configuration.GetSection(JobsOptions.SectionName));
    services.AddLogging(logging => logging.AddConsole());
    services.AddScoped<CronSyncService>();
    services.AddScoped<WatchdogService>();

    await using var provider = services.BuildServiceProvider();
    // IUnitOfWork (UnitOfWork) implements only IAsyncDisposable, not IDisposable - a plain
    // synchronous `using var scope = provider.CreateScope()` throws on dispose ("type only
    // implements IAsyncDisposable") once such a service has been resolved into the scope.
    await using var scope = provider.CreateAsyncScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("QueryPlus.SchedulerSync");

    if (mode == RunMode.Sync)
    {
        logger.LogInformation("Starting cron sync pass.");
        await scope.ServiceProvider.GetRequiredService<CronSyncService>().RunAsync();
        logger.LogInformation("Cron sync pass completed successfully.");
    }
    else
    {
        logger.LogInformation("Starting watchdog pass.");
        await scope.ServiceProvider.GetRequiredService<WatchdogService>().RunAsync();
        logger.LogInformation("Watchdog pass completed successfully.");
    }

    return 0;
}
catch (Exception ex)
{
    // No ILogger may exist yet if this failed during bootstrap (e.g. OpenBao unreachable, or the
    // connection string is missing) - stderr is the guaranteed-available fallback either way.
    Console.Error.WriteLine($"QueryPlus.SchedulerSync failed: {ex}");
    return 1;
}

static RunMode? ParseMode(string[] args)
{
    if (args.Length == 0)
    {
        return RunMode.Sync;
    }

    if (args.Length == 1 && args[0] == "--sync")
    {
        return RunMode.Sync;
    }

    if (args.Length == 1 && args[0] == "--watchdog")
    {
        return RunMode.Watchdog;
    }

    return null;
}

internal enum RunMode
{
    Sync,
    Watchdog
}
