namespace QueryPlus.Data.Interceptors;

/// <summary>
/// Provides the current user context for audit revisions.
/// Implement in the Web layer (HTTP claims / connection IP).
/// </summary>
public interface IAuditContext
{
    string Username { get; }
    string? IpAddress { get; }
}
