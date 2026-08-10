using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueryPlus.Application.DependencyInjection;
using QueryPlus.Application.Options;
using QueryPlus.Hosting;
using QueryPlus.Infrastructure.DependencyInjection;
using QueryPlus.Runner;

EnvFileLoader.LoadFromAncestors(Directory.GetCurrentDirectory());
EnvFileLoader.LoadFromAncestors(AppContext.BaseDirectory);
await OpenBaoSecretLoader.LoadFromEnvironmentAsync();

RunnerArgs runnerArgs;
try
{
    runnerArgs = RunnerArgs.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"QueryPlus.Runner: invalid arguments: {ex.Message}");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddApplication();
services.AddInfrastructure(configuration);
services.AddOptions<JobsOptions>().Bind(configuration.GetSection(JobsOptions.SectionName));
services.AddLogging(logging => logging.AddConsole());

await using var serviceProvider = services.BuildServiceProvider();

var runnerHost = new JobRunnerHost(serviceProvider);
return await runnerHost.RunAsync(runnerArgs);
