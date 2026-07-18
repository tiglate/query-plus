using System.Text.Json;

namespace QueryPlus.Application.Common;

public static class JsonHelpers
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static IReadOnlyList<string> ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json, Options);
            return list?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList()
                   ?? (IReadOnlyList<string>)[];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(value, Options);
    }

    public static bool IsValidStringArrayJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            JsonSerializer.Deserialize<List<string>>(json, Options);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
