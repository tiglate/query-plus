namespace QueryPlus.Api.Hosting;

public static class EnvFileLoader
{
    public static void LoadFromAncestors(string startDirectory, string fileName = ".env")
    {
        if (string.IsNullOrWhiteSpace(startDirectory)) return;
        for (DirectoryInfo? directory = new(startDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, fileName);
            if (File.Exists(path))
            {
                Load(path);
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
