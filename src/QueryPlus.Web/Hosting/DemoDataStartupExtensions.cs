using QueryPlus.Data.Seed;

namespace QueryPlus.Web.Hosting;

/// <summary>
/// One-shot startup seeding (migrations + demo catalog/SQL objects).
/// </summary>
public static class DemoDataStartupExtensions
{
    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo data seeding failed. The app will start, but demo procedures may be unavailable.");
            if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Docker"))
            {
                throw;
            }
        }
    }
}
