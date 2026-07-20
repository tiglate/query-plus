using Microsoft.Data.SqlClient;

namespace QueryPlus.Web.Services;

/// <summary>
/// Safe, non-secret connection summary for the footer (helps distinguish DEV vs PROD).
/// </summary>
public sealed class DatabaseConnectionDisplay
{
    public string Server { get; }
    public string Database { get; }

    public DatabaseConnectionDisplay(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Server = "—";
            Database = "—";
            return;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            Server = string.IsNullOrWhiteSpace(builder.DataSource) ? "—" : builder.DataSource;
            Database = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "—" : builder.InitialCatalog;
        }
        catch
        {
            Server = "—";
            Database = "—";
        }
    }

    /// <summary>Compact label for the footer, e.g. <c>localhost,1433 · QueryPlus</c>.</summary>
    public string FooterLabel => $"{Server} · {Database}";
}
