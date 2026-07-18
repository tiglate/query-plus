using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace QueryPlus.Data.Context;

/// <summary>
/// Design-time factory for EF Core migrations.
/// </summary>
public class QueryPlusDbContextFactory : IDesignTimeDbContextFactory<QueryPlusDbContext>
{
    public QueryPlusDbContext CreateDbContext(string[] args)
    {
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

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=QueryPlus;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False";

        var optionsBuilder = new DbContextOptionsBuilder<QueryPlusDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new QueryPlusDbContext(optionsBuilder.Options);
    }
}
