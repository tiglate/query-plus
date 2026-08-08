using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QueryPlus.Api.Auth;

namespace QueryPlus.Api.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Keycloak");
        var authority = section["Authority"] ?? "http://localhost:8080/realms/queryplus";
        var metadataAddress = section["MetadataAddress"];
        var backchannelHost = section["BackchannelHost"];
        var backchannelPort = section.GetValue("BackchannelPort", 8080);
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "QueryPlus.Auth";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = authority;
                if (!string.IsNullOrWhiteSpace(metadataAddress)) options.MetadataAddress = metadataAddress;
                options.ClientId = section["ClientId"];
                options.ClientSecret = section["ClientSecret"];
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.RequireHttpsMetadata = section.GetValue("RequireHttpsMetadata", true);
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.TokenValidationParameters.NameClaimType = "preferred_username";
                // Keycloak's token carries roles under the short claim name "roles", but the
                // token handler's default inbound claim-type mapping (the same mechanism that
                // remaps "sub" -> ClaimTypes.NameIdentifier, "email" -> ClaimTypes.Email, etc.)
                // rewrites it to the long ClaimTypes.Role URI before any event below ever sees
                // the principal - so RoleClaimType must point at ClaimTypes.Role (the default),
                // not the literal "roles" string, or ClaimsPrincipal.IsInRole /
                // [Authorize(Roles=...)] silently never matches anything.
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                options.TokenValidationParameters.ValidIssuer = authority;
                options.TokenValidationParameters.ValidateIssuer = true;
                if (!string.IsNullOrWhiteSpace(backchannelHost) &&
                    Uri.TryCreate(authority, UriKind.Absolute, out var uri))
                    options.BackchannelHttpHandler = new KeycloakBackchannelHttpHandler(uri.Host,
                        uri.IsDefaultPort ? uri.Scheme == "https" ? 443 : 80 : uri.Port, backchannelHost,
                        backchannelPort);
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        KeycloakUrlRewriter.RewriteBrowserFacingIssuer(context.ProtocolMessage, authority);
                        return Task.CompletedTask;
                    },
                    OnRedirectToIdentityProviderForSignOut = context =>
                    {
                        KeycloakUrlRewriter.RewriteBrowserFacingIssuer(context.ProtocolMessage, authority);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // Defensive fallback: if some future claim source sends the singular
                        // "role" key (unmapped) instead of "roles", normalize it - a no-op today
                        // since Keycloak's "roles" claim is already inbound-mapped to
                        // ClaimTypes.Role by this point (see RoleClaimType above).
                        if (context.Principal?.Identity is ClaimsIdentity identity &&
                            !identity.HasClaim(x => x.Type is "roles" || x.Type == ClaimTypes.Role))
                            foreach (var role in context.Principal.FindAll("role"))
                                identity.AddClaim(new Claim(ClaimTypes.Role, role.Value));
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }
}
