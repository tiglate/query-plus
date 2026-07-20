namespace QueryPlus.Web.Hosting;

/// <summary>
/// Loads a dotenv-style file into process environment variables (without overriding
/// variables that are already set). Used so local <c>.env</c> works with
/// <c>dotnet run</c> the same way Docker Compose uses it.
/// </summary>
public static class EnvFileLoader
{
    /// <summary>
    /// Walks from <paramref name="startDirectory"/> toward the filesystem root
    /// and loads the first <paramref name="fileName"/> found (typically <c>.env</c>).
    /// </summary>
    public static void LoadFromAncestors(string startDirectory, string fileName = ".env")
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return;
        }

        DirectoryInfo? dir = new(startDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, fileName);
            if (File.Exists(path))
            {
                Load(path);
                return;
            }

            dir = dir.Parent;
        }
    }

    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            // Support optional "export KEY=value"
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
            if (key.Length == 0)
            {
                continue;
            }

            // Do not clobber variables already set by the host / CI / Compose.
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                continue;
            }

            var value = line[(eq + 1)..].Trim();
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
