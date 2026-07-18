using System.Security.Claims;
using QueryPlus.Application.Abstractions;

namespace QueryPlus.Web.Infrastructure;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public string Username
    {
        get
        {
            var user = User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return "anonymous";
            }

            return user.FindFirst("preferred_username")?.Value
                   ?? user.Identity?.Name
                   ?? user.FindFirst(ClaimTypes.Name)?.Value
                   ?? user.FindFirst(ClaimTypes.Email)?.Value
                   ?? "unknown";
        }
    }

    public string? IpAddress
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null)
            {
                return null;
            }

            var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                return forwarded.Split(',')[0].Trim();
            }

            return ctx.Connection.RemoteIpAddress?.ToString();
        }
    }

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var user = User;
            if (user is null)
            {
                return [];
            }

            // Keycloak may emit roles as multi-claim "roles" or realm_access.
            var roles = user.FindAll("roles")
                .Concat(user.FindAll(ClaimTypes.Role))
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return roles;
        }
    }

    public bool IsInRole(string role)
        => Roles.Contains(role, StringComparer.OrdinalIgnoreCase)
           || (User?.IsInRole(role) ?? false);
}
