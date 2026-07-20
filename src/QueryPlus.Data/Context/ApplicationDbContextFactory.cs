using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace QueryPlus.Data.Context;

/// <summary>
/// Design-time factory for EF Core migrations (dotnet ef).
/// Loads repo-root <c>.env</c> when present (same dummy-local pattern as the web host).
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        LoadEnvFromAncestors(Directory.GetCurrentDirectory());

        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "QueryPlus.Web");
        if (!Directory.Exists(basePath))
        {
            basePath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not set. " +
                "Copy .env.example to .env at the repo root (or set ConnectionStrings__DefaultConnection). " +
                "Do not use dummy .env credentials outside local development.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    /// <summary>Minimal dotenv loader (design-time; mirrors Web EnvFileLoader behaviour).</summary>
    private static void LoadEnvFromAncestors(string startDirectory)
    {
        DirectoryInfo? dir = new(startDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (File.Exists(path))
            {
                foreach (var raw in File.ReadLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith('#'))
                    {
                        continue;
                    }

                    if (line.StartsWith("export ", StringComparison.Ordinal))
                    {
                        line = line["export ".Length..].TrimStart();
                    }

                    var eq = line.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }

                    var key = line[..eq].Trim();
                    if (key.Length == 0
                        || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    {
                        continue;
                    }

                    var value = line[(eq + 1)..].Trim();
                    if (value.Length >= 2
                        && ((value[0] == '"' && value[^1] == '"')
                            || (value[0] == '\'' && value[^1] == '\'')))
                    {
                        value = value[1..^1];
                    }

                    Environment.SetEnvironmentVariable(key, value);
                }

                return;
            }

            dir = dir.Parent;
        }
    }
}
