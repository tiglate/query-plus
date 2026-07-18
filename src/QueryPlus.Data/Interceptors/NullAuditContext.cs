namespace QueryPlus.Data.Interceptors;

/// <summary>
/// Fallback when no HTTP user is available (migrations, background jobs, tests).
/// </summary>
public sealed class NullAuditContext : IAuditContext
{
    public string Username => "system";
    public string? IpAddress => null;
}
