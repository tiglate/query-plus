using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QueryPlus.Application.Common;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Seed;

/// <summary>
/// Applies EF migrations, then - only when demo data seeding is enabled and safe - installs demo
/// SQL objects and registers catalog metadata so the app starts with end-to-end test procedures.
/// </summary>
public sealed class DemoDataSeeder(
    ApplicationDbContext db,
    IConfiguration configuration,
    ILogger<DemoDataSeeder> logger)
{
    private const string EfMigrationsHistoryTable = "__EFMigrationsHistory";

    private static readonly Regex DemoTableNameRegex =
        new(@"CREATE TABLE\s+dbo\.(\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (db.Database.IsRelational())
        {
            logger.LogInformation("Applying database migrations…");
            await db.Database.MigrateAsync(cancellationToken);
        }

        // Database:SeedDemoDataOnStartup defaults to false (see appsettings.json) and is only
        // flipped to true for Development/Docker (appsettings.{Environment}.json) - a Production
        // deploy that never sets ASPNETCORE_ENVIRONMENT=Development/Docker gets no demo data,
        // with no extra opt-out step required.
        if (!configuration.GetValue("Database:SeedDemoDataOnStartup", false))
        {
            logger.LogInformation(
                "Demo data seeding is disabled (Database:SeedDemoDataOnStartup=false); skipping.");
            return;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' is not configured.");

        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            databaseName = "QueryPlus";
        }

        // Independent safety net: even if the flag above is true, never touch a database that
        // already has tables QueryPlus didn't create - it's shared with something else.
        if (db.Database.IsRelational() && await HasForeignTablesAsync(connectionString, cancellationToken))
        {
            logger.LogWarning(
                "Database {Database} already contains tables that QueryPlus did not create; " +
                "skipping demo data seed to avoid touching a shared/existing database.", databaseName);
            return;
        }

        try
        {
            await InstallSqlObjectsAsync(connectionString, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not install demo SQL objects (database may be unreachable or non-SQL Server).");
        }

        await SeedCatalogAsync(databaseName, cancellationToken);

        logger.LogInformation("Demo data seed completed for database {Database}.", databaseName);
    }

    /// <summary>
    /// True when the database has any base table that isn't one QueryPlus itself owns - either
    /// an EF-migrated catalog/audit table or a demo object installed by <c>demo-objects.sql</c>.
    /// The known-table set is derived from the live EF model and the seed script itself, so it
    /// can't drift out of sync as entities or demo objects are added.
    /// </summary>
    private async Task<bool> HasForeignTablesAsync(string connectionString, CancellationToken cancellationToken)
    {
        var knownTables = GetKnownTableNames();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!knownTables.Contains(reader.GetString(0)))
            {
                return true;
            }
        }

        return false;
    }

    private HashSet<string> GetKnownTableNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { EfMigrationsHistoryTable };

        foreach (var entityType in db.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                names.Add(tableName);
            }
        }

        var demoObjectsPath = ResolveSeedFile("demo-objects.sql");
        if (File.Exists(demoObjectsPath))
        {
            var script = File.ReadAllText(demoObjectsPath);
            foreach (Match match in DemoTableNameRegex.Matches(script))
            {
                names.Add(match.Groups[1].Value);
            }
        }

        return names;
    }

    private async Task InstallSqlObjectsAsync(string connectionString, CancellationToken cancellationToken)
    {
        var sqlPath = ResolveSeedFile("demo-objects.sql");
        if (!File.Exists(sqlPath))
        {
            logger.LogWarning("Demo SQL file not found at {Path}; skipping SQL object install.", sqlPath);
            return;
        }

        var script = await File.ReadAllTextAsync(sqlPath, cancellationToken);
        var batches = SplitBatches(script);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        logger.LogInformation("Installing {Count} demo SQL batches…", batches.Count);
        foreach (var batch in batches)
        {
            await using var cmd = new SqlCommand(batch, connection)
            {
                CommandTimeout = 120
            };
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task SeedCatalogAsync(string databaseName, CancellationToken cancellationToken)
    {
        var catalogPath = ResolveSeedFile("demo-catalog.json");
        if (!File.Exists(catalogPath))
        {
            logger.LogWarning("Demo catalog file not found at {Path}; skipping catalog seed.", catalogPath);
            return;
        }

        var json = await File.ReadAllTextAsync(catalogPath, cancellationToken);
        var entries = JsonSerializer.Deserialize<List<CatalogEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        if (entries.Count == 0)
        {
            return;
        }

        // Ensure categories exist for every catalog entry.
        var categories = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        foreach (var categoryName in entries.Select(e => e.Category).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existing = await db.Categories
                .FirstOrDefaultAsync(c => c.Description == categoryName, cancellationToken);
            if (existing is null)
            {
                existing = new Category { Description = categoryName, CreatedAt = DateTime.UtcNow };
                db.Categories.Add(existing);
                await db.SaveChangesAsync(cancellationToken);
            }

            categories[categoryName] = existing;
        }

        // Idempotent: only insert procedures that are not already registered (by technical name).
        var existingNames = await db.Procedures
            .AsNoTracking()
            .Select(p => p.ProcedureName)
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var entry in entries)
        {
            if (existingSet.Contains(entry.ProcedureName)
                || existingSet.Contains(entry.ProcedureName.Replace("dbo.", "", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Also treat bare vs dbo-prefixed as the same.
            var bare = entry.ProcedureName.Contains('.')
                ? entry.ProcedureName[(entry.ProcedureName.LastIndexOf('.') + 1)..]
                : entry.ProcedureName;
            if (existingSet.Contains(bare) || existingSet.Contains("dbo." + bare))
            {
                continue;
            }

            if (!categories.TryGetValue(entry.Category, out var category))
            {
                continue;
            }

            var procedure = BuildProcedureFromCatalog(entry, category.IdCategory, databaseName);
            db.Procedures.Add(procedure);
            existingSet.Add(entry.ProcedureName);
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} new demo procedure(s) into catalog.", added);
        }
        else
        {
            logger.LogInformation("Demo catalog up to date; no new procedures to seed.");
        }

        await BackfillRequiredFlagsAsync(cancellationToken);
    }

    private static Procedure BuildProcedureFromCatalog(
        CatalogEntry entry,
        int categoryId,
        string databaseName)
    {
        var procedure = new Procedure
        {
            IdCategory = categoryId,
            Caption = entry.Caption,
            ConnectionName = "DefaultConnection",
            DatabaseName = databaseName,
            ProcedureName = entry.ProcedureName,
            Enabled = true,
            SupportsPagination = entry.SupportsPagination,
            RoleEntitlement = string.IsNullOrWhiteSpace(entry.Role) ? "ROLE_QUERY_EXEC" : entry.Role,
            Description = entry.Description,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var p in entry.Parameters)
        {
            // Reserved pagination args are system-injected; never catalog them as user parameters.
            if (ProcedurePagination.IsReservedParameterName(p.Name))
            {
                continue;
            }

            var paramType = ParseParameterType(p.Type);
            // Prefer explicit flag; otherwise treat missing default as required (except boolean).
            var isRequired = p.Required
                             ?? (paramType != ParameterType.Boolean && string.IsNullOrWhiteSpace(p.Default));

            procedure.Parameters.Add(new ProcedureParameter
            {
                Caption = p.Caption,
                Name = p.Name.StartsWith('@') ? p.Name : "@" + p.Name,
                ParameterType = paramType,
                DefaultValue = p.Default,
                ComboValues = p.Combo,
                IsRequired = isRequired,
                CreatedAt = DateTime.UtcNow
            });
        }

        foreach (var c in entry.Columns)
        {
            procedure.Columns.Add(new ProcedureColumn
            {
                TechnicalName = c.Tech,
                Caption = c.Caption,
                Alignment = ParseAlignment(c.Align),
                FormatMask = c.Format,
                Visible = c.Visible,
                CreatedAt = DateTime.UtcNow
            });
        }

        return procedure;
    }

    private static string ResolveSeedFile(string fileName)
    {
        // Prefer content next to the assembly (copied on build), then project-relative paths.
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "QueryPlus.Data", "Seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "QueryPlus.Data", "Seed", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "QueryPlus.Data", "Seed",
                fileName)
        };

        return candidates.FirstOrDefault(File.Exists)
               ?? Path.Combine(AppContext.BaseDirectory, "Seed", fileName);
    }

    private static IReadOnlyList<string> SplitBatches(string script)
    {
        // Split on lines that are only GO (case-insensitive).
        var batches = new List<string>();
        var current = new List<string>();

        foreach (var line in script.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                continue;
            }

            current.Add(line.TrimEnd('\r'));
        }

        Flush();
        return batches;

        void Flush()
        {
            var batch = string.Join('\n', current).Trim();
            current.Clear();
            if (!string.IsNullOrWhiteSpace(batch) && !batch.StartsWith("-- QueryPlus demo", StringComparison.Ordinal))
            {
                // Keep comment-only first batch if it has real SQL after comments
            }

            if (!string.IsNullOrWhiteSpace(batch))
            {
                // Skip batches that are only comments
                var withoutComments = string.Join('\n',
                    batch.Split('\n').Where(l => !l.TrimStart().StartsWith("--", StringComparison.Ordinal)));
                if (!string.IsNullOrWhiteSpace(withoutComments))
                {
                    batches.Add(batch);
                }
            }
        }
    }

    /// <summary>
    /// For existing installs, mark non-boolean parameters without a default as required.
    /// </summary>
    private async Task BackfillRequiredFlagsAsync(CancellationToken cancellationToken)
    {
        var paramsToUpdate = await db.ProcedureParameters
            .Where(p => !p.IsRequired
                        && p.ParameterType != ParameterType.Boolean
                        && (p.DefaultValue == null || p.DefaultValue == ""))
            .ToListAsync(cancellationToken);

        if (paramsToUpdate.Count == 0)
        {
            return;
        }

        foreach (var p in paramsToUpdate)
        {
            p.IsRequired = true;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Backfilled IsRequired on {Count} parameters.", paramsToUpdate.Count);
    }

    private static ParameterType ParseParameterType(string? type)
        => Enum.TryParse<ParameterType>(type, ignoreCase: true, out var value)
            ? value
            : ParameterType.FreeText;

    private static ColumnAlignment ParseAlignment(string? align)
        => Enum.TryParse<ColumnAlignment>(align, ignoreCase: true, out var value)
            ? value
            : ColumnAlignment.Left;

    private sealed class CatalogEntry
    {
        public string Caption { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public string Role { get; set; } = "user";
        public bool SupportsPagination { get; set; }
        public List<CatalogParameter> Parameters { get; set; } = [];
        public List<CatalogColumn> Columns { get; set; } = [];
    }

    private sealed class CatalogParameter
    {
        public string Caption { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "FreeText";
        public string? Default { get; set; }
        public string? Combo { get; set; }
        public bool? Required { get; set; }
    }

    private sealed class CatalogColumn
    {
        public string Tech { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string Align { get; set; } = "Left";
        public string? Format { get; set; }
        public bool Visible { get; set; } = true;
    }
}
