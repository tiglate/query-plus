namespace QueryPlus.Api.Hosting;

public static class EnvFileLoader
{
    /// <summary>
    /// Marks the repository root; the ancestor search below never walks above the directory
    /// containing this file, so an unrelated .env sitting further up the filesystem (a parent
    /// workspace, CI checkout root, or a developer's home directory) can never be picked up.
    /// </summary>
    private const string RepoRootMarker = "QueryPlus.sln";

    /// <summary>
    /// Absolute fallback bound for environments where the marker file isn't present in the
    /// ancestry at all (e.g. a published container image, which only ships the app's own
    /// output directory) - keeps the walk from reaching the filesystem root regardless.
    /// </summary>
    private const int MaxLevels = 8;

    public static void LoadFromAncestors(string startDirectory, string fileName = ".env")
    {
        if (string.IsNullOrWhiteSpace(startDirectory)) return;
        var directory = new DirectoryInfo(startDirectory);
        for (var level = 0; directory is not null && level < MaxLevels; level++, directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, fileName);
            if (File.Exists(path))
            {
                Load(path);
                return;
            }

            if (File.Exists(Path.Combine(directory.FullName, RepoRootMarker)))
            {
                return;
            }
        }
    }

    public static void Load(string path)
    {
        if (!File.Exists(path)) return;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();
            var separator = line.IndexOf('=');
            if (separator <= 0) continue;
            var key = line[..separator].Trim();
            if (key.Length == 0 || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))) continue;
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                value = value[1..^1];
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
