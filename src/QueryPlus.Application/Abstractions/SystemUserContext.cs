namespace QueryPlus.Application.Abstractions;

/// <summary>
/// Default non-interactive user (tests, background jobs, migrations).
/// Register a real implementation from Web that reads claims/HTTP context.
/// </summary>
public sealed class SystemUserContext : ICurrentUserContext
{
    public bool IsAuthenticated => true;
    public string Username => "system";
    public string? IpAddress => null;
    public IReadOnlyCollection<string> Roles { get; } = ["admin", "user"];

    public bool IsInRole(string role)
        => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
