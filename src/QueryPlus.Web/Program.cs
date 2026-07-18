using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DependencyInjection;
using QueryPlus.Application.Interfaces;
using QueryPlus.Data.Interceptors;
using QueryPlus.Data.Seed;
using QueryPlus.Infrastructure.DependencyInjection;
using QueryPlus.Web.Infrastructure;
using QueryPlus.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddScoped<IAuditContext, HttpAuditContext>();

builder.Services.AddSingleton<ExcelExportService>();
builder.Services.AddSingleton<IExcelExportService>(sp => sp.GetRequiredService<ExcelExportService>());
builder.Services.AddSingleton<ExportEligibilityService>();
builder.Services.AddHostedService<ExcelExportBackgroundService>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Culture");
});

builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("pt-BR"),
        new CultureInfo("en")
    };

    options.DefaultRequestCulture = new RequestCulture("pt-BR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});

var keycloakSection = builder.Configuration.GetSection("Keycloak");
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

builder.Services
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
                RewriteBrowserFacingKeycloakUrl(context.ProtocolMessage, authority);
                return Task.CompletedTask;
            },
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                RewriteBrowserFacingKeycloakUrl(context.ProtocolMessage, authority);
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

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure schema + demo tables/procedures/catalog exist before serving traffic.
await using (var scope = app.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Demo data seeding failed. The app will start, but demo procedures may be unavailable.");
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Docker"))
        {
            throw;
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.MapGet("/Account/Login", (string? returnUrl) =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl },
        [OpenIdConnectDefaults.AuthenticationScheme]))
    .AllowAnonymous();

app.MapPost("/Account/Logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
}).RequireAuthorization().DisableAntiforgery();

app.MapGet("/exports/download/{jobId:guid}", (Guid jobId, IExcelExportService exports) =>
{
    var path = exports.GetFilePath(jobId);
    if (path is null)
    {
        return Results.NotFound();
    }

    var job = exports.GetJob(jobId);
    var downloadName = job?.FileName ?? Path.GetFileName(path);
    return Results.File(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", downloadName);
}).RequireAuthorization();

app.Run();

static void RewriteBrowserFacingKeycloakUrl(
    OpenIdConnectMessage message,
    string publicAuthority)
{
    if (!Uri.TryCreate(publicAuthority, UriKind.Absolute, out var publicAuthorityUri))
    {
        return;
    }

    static string? Rewrite(string? url, Uri publicAuthorityUri)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        // Docker-internal service name must never appear in browser redirects.
        if (!uri.Host.Equals("keycloak", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = publicAuthorityUri.Scheme,
            Host = publicAuthorityUri.Host,
            Port = publicAuthorityUri.IsDefaultPort ? -1 : publicAuthorityUri.Port
        };
        return builder.Uri.ToString();
    }

    message.IssuerAddress = Rewrite(message.IssuerAddress, publicAuthorityUri) ?? message.IssuerAddress;
}

public partial class Program;
