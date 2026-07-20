using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QueryPlus.Web.Auth;

namespace QueryPlus.Web.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddWebAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloakSection = configuration.GetSection("Keycloak");
        // Browser-facing issuer (must resolve on the host machine, e.g. localhost).
        var authority = keycloakSection["Authority"]
                        ?? "http://localhost:8080/realms/queryplus";
        // Container-internal discovery document (Docker DNS name). Optional.
        var metadataAddress = keycloakSection["MetadataAddress"];
        var clientId = keycloakSection["ClientId"];
        var clientSecret = keycloakSection["ClientSecret"];
        var requireHttpsMetadata = keycloakSection.GetValue("RequireHttpsMetadata", true);
        // When the app runs inside Docker, rewrite localhost back-channel calls to this host.
        var backchannelHost = keycloakSection["BackchannelHost"]; // e.g. "keycloak"
        var backchannelPort = keycloakSection.GetValue("BackchannelPort", 8080);

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "QueryPlus.Auth";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.LoginPath = "/Account/Login";
            })
            .AddOpenIdConnect(options =>
            {
                // Authority is what users' browsers hit and what tokens should list as issuer.
                options.Authority = authority;
                if (!string.IsNullOrWhiteSpace(metadataAddress))
                {
                    // Fetch OIDC metadata via Docker network while keeping public Authority for redirects.
                    options.MetadataAddress = metadataAddress;
                }

                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.RoleClaimType = "roles";
                // Accept issuer as the public Authority even if discovery was loaded from the internal host.
                options.TokenValidationParameters.ValidIssuer = authority;
                options.TokenValidationParameters.ValidateIssuer = true;

                if (!string.IsNullOrWhiteSpace(backchannelHost)
                    && Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri))
                {
                    // Token / JWKS / userinfo calls from inside the container must not use localhost.
                    options.BackchannelHttpHandler = new KeycloakBackchannelHttpHandler(
                        publicHost: authorityUri.Host,
                        publicPort: authorityUri.IsDefaultPort
                            ? (authorityUri.Scheme == Uri.UriSchemeHttps ? 443 : 80)
                            : authorityUri.Port,
                        internalHost: backchannelHost,
                        internalPort: backchannelPort);
                }

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        // Never send the browser to the Docker-internal hostname.
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
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            if (!identity.HasClaim(c => c.Type == "roles" || c.Type == ClaimTypes.Role))
                            {
                                foreach (var role in context.Principal.FindAll("role"))
                                {
                                    identity.AddClaim(new Claim("roles", role.Value));
                                }
                            }
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
