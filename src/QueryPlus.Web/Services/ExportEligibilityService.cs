using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QueryPlus.Web.Services;

/// <summary>
/// Tracks whether the current user has a recent successful Execute with rows,
/// so Export Excel cannot be invoked without prior result data.
/// </summary>
public sealed class ExportEligibilityService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, EligibilityEntry> _entries = new(StringComparer.Ordinal);

    public void MarkEligible(
        string username,
        int procedureId,
        IDictionary<string, string?> parameterValues,
        int rowCount)
    {
        if (string.IsNullOrWhiteSpace(username) || procedureId <= 0 || rowCount <= 0)
        {
            Clear(username);
            return;
        }

        _entries[Key(username)] = new EligibilityEntry(
            procedureId,
            HashParameters(parameterValues),
            rowCount,
            DateTime.UtcNow);
    }

    public void Clear(string username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            _entries.TryRemove(Key(username), out _);
        }
    }

    public bool TryValidate(
        string username,
        int procedureId,
        IDictionary<string, string?> parameterValues,
        out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(username) || procedureId <= 0)
        {
            error = "export-not-eligible";
            return false;
        }

        if (!_entries.TryGetValue(Key(username), out var entry))
        {
            error = "export-not-eligible";
            return false;
        }

        if (DateTime.UtcNow - entry.CreatedAt > Ttl)
        {
            _entries.TryRemove(Key(username), out _);
            error = "export-expired";
            return false;
        }

        if (entry.ProcedureId != procedureId)
        {
            error = "export-procedure-mismatch";
            return false;
        }

        if (entry.RowCount <= 0)
        {
            error = "export-no-rows";
            return false;
        }

        var hash = HashParameters(parameterValues);
        if (!string.Equals(hash, entry.ParameterHash, StringComparison.Ordinal))
        {
            error = "export-params-mismatch";
            return false;
        }

        return true;
    }

    private static string Key(string username) => username.Trim().ToLowerInvariant();

    private static string HashParameters(IDictionary<string, string?> parameterValues)
    {
        var ordered = parameterValues
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"{kv.Key.ToLowerInvariant()}={(kv.Value ?? string.Empty).Trim()}");
        var payload = string.Join("&", ordered);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private sealed record EligibilityEntry(
        int ProcedureId,
        string ParameterHash,
        int RowCount,
        DateTime CreatedAt);
}
