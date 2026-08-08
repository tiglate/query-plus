using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QueryPlus.Api.Tests.Infrastructure;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var roles = Request.Headers.TryGetValue(RolesHeader, out var value) && value.Count > 0
            ? value.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : ["ROLE_ADMIN"];

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test-user"),
            new("preferred_username", "test-user"),
            new(ClaimTypes.NameIdentifier, "test-user-id")
        };
        claims.AddRange(roles.Select(role => new Claim("roles", role)));

        // roleType must match Keycloak:RoleClaimType ("roles", see AuthenticationServiceCollectionExtensions)
        // so ClaimsPrincipal.IsInRole/[Authorize(Roles=...)] resolve the same way they do in production.
        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, "roles");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
