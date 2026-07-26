using QueryPlus.Data.Seed;

namespace QueryPlus.Api.Hosting;

public static class DemoDataStartupExtensions
{
    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
        }
        catch (Exception ex)
        {
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup").LogError(ex,
                "Demo data seeding failed. The app will start, but demo procedures may be unavailable.");
            if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Docker")) throw;
        }
    }
}
