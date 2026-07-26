using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace QueryPlus.Api.Services;

public sealed class ExportEligibilityService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.Ordinal);
    public void MarkEligible(string username, int procedureId, IDictionary<string, string?> values, int rowCount)
    {
        if (string.IsNullOrWhiteSpace(username) || procedureId <= 0 || rowCount <= 0) { Clear(username); return; }
        entries[Key(username)] = new(procedureId, Hash(values), rowCount, DateTime.UtcNow);
    }
    public void Clear(string username) { if (!string.IsNullOrWhiteSpace(username)) entries.TryRemove(Key(username), out _); }
    public bool TryValidate(string username, int procedureId, IDictionary<string, string?> values, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(username) || procedureId <= 0 || !entries.TryGetValue(Key(username), out var entry)) { error = "export-not-eligible"; return false; }
        if (DateTime.UtcNow - entry.CreatedAt > Ttl) { entries.TryRemove(Key(username), out _); error = "export-expired"; return false; }
        if (entry.ProcedureId != procedureId) { error = "export-procedure-mismatch"; return false; }
        if (entry.RowCount <= 0) { error = "export-no-rows"; return false; }
        if (Hash(values) != entry.ParameterHash) { error = "export-params-mismatch"; return false; }
        return true;
    }
    private static string Key(string username) => username.Trim().ToLowerInvariant();
    private static string Hash(IDictionary<string, string?> values) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("&", values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => $"{x.Key.ToLowerInvariant()}={(x.Value ?? string.Empty).Trim()}")))));
    private sealed record Entry(int ProcedureId, string ParameterHash, int RowCount, DateTime CreatedAt);
}
