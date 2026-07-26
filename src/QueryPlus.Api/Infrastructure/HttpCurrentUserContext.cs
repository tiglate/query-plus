using System.Security.Claims;
using QueryPlus.Application.Abstractions;

namespace QueryPlus.Api.Infrastructure;

public sealed class HttpCurrentUserContext(IHttpContextAccessor accessor) : ICurrentUserContext
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public string Username => IsAuthenticated
        ? User?.FindFirst("preferred_username")?.Value ?? User?.Identity?.Name ?? "unknown"
        : "anonymous";

    public string? IpAddress
    {
        get
        {
            var context = accessor.HttpContext;
            var forwarded = context?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return !string.IsNullOrWhiteSpace(forwarded)
                ? forwarded.Split(',')[0].Trim()
                : context?.Connection.RemoteIpAddress?.ToString();
        }
    }

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var user = User;
            if (user is null) return [];
            return user.FindAll("roles")
                .Concat(user.FindAll(ClaimTypes.Role))
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase) || (User?.IsInRole(role) ?? false);
}
