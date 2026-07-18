using QueryPlus.Application.Abstractions;
using QueryPlus.Data.Interceptors;

namespace QueryPlus.Web.Infrastructure;

public sealed class HttpAuditContext : IAuditContext
{
    private readonly ICurrentUserContext _user;

    public HttpAuditContext(ICurrentUserContext user)
    {
        _user = user;
    }

    public string Username => string.IsNullOrWhiteSpace(_user.Username) ? "system" : _user.Username;
    public string? IpAddress => _user.IpAddress;
}
