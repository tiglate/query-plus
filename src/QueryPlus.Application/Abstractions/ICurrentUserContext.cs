namespace QueryPlus.Application.Abstractions;

/// <summary>
/// Abstraction over the authenticated user (implemented in Web from claims).
/// </summary>
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    string Username { get; }
    string? IpAddress { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
}
