using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace QueryPlus.Hosting;

/// <summary>
/// Fetches app secrets from an OpenBao (Vault-API-compatible) KV v2 store and merges them into
/// the process environment, mirroring EnvFileLoader's non-destructive precedence (an
/// already-set env var always wins). Bootstrap values (address + token) can't themselves come
/// from OpenBao - they're read from the environment, exactly like EnvFileLoader is itself
/// seeded by the shell/CI before .env is consulted.
/// </summary>
public static class OpenBaoSecretLoader
{
    private const string MountPoint = "secret";
    private const string SecretPath = "queryplus";

    /// <summary>
    /// Reads OPENBAO_ADDR/OPENBAO_TOKEN from the environment and, if both are present, fetches
    /// and applies the KV v2 secret at secret/queryplus. No-ops silently if either is unset, so
    /// environments that don't use OpenBao (e.g. the fast test suite) are unaffected.
    /// </summary>
    public static async Task LoadFromEnvironmentAsync()
    {
        var address = Environment.GetEnvironmentVariable("OPENBAO_ADDR");
        var token = Environment.GetEnvironmentVariable("OPENBAO_TOKEN");
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        IReadOnlyDictionary<string, string> secrets;
        try
        {
            secrets = await FetchSecretsAsync(address, token, MountPoint, SecretPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch secrets from OpenBao at '{address}'.", ex);
        }

        foreach (var (key, value) in secrets)
        {
            if (key.Length > 0 && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    /// <summary>
    /// Reads a KV v2 secret and returns its entries as strings. Pure and side-effect free (no
    /// environment mutation), so it can be unit/integration-tested against a real OpenBao
    /// instance without touching process state.
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, string>> FetchSecretsAsync(
        string address,
        string token,
        string mountPoint,
        string secretPath)
    {
        IAuthMethodInfo authMethod = new TokenAuthMethodInfo(token);
        var client = new VaultClient(new VaultClientSettings(address, authMethod));

        var secret = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath, mountPoint: mountPoint);

        return secret.Data.Data.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty);
    }
}
