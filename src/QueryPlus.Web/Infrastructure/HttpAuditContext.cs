using QueryPlus.Application.Abstractions;
using QueryPlus.Data.Interceptors;

namespace QueryPlus.Web.Infrastructure;

public sealed class HttpAuditContext(ICurrentUserContext user) : IAuditContext
{
    public string Username => string.IsNullOrWhiteSpace(user.Username) ? "system" : user.Username;
    public string? IpAddress => user.IpAddress;
}
