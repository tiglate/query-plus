using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Api.Tests.Infrastructure;

public sealed class AnonymousQueryPlusApiApplicationFactory : WebApplicationFactory<Program>
{
    public IProcedureService Procedures { get; } = Substitute.For<IProcedureService>();
    public ICategoryService Categories { get; } = Substitute.For<ICategoryService>();
    public IExecutionService Execution { get; } = Substitute.For<IExecutionService>();
    public IExcelExportService Exports { get; } = Substitute.For<IExcelExportService>();
    public IProcedureRepository ProcedureRepository { get; } = Substitute.For<IProcedureRepository>();
    public IProcedureMetadataSyncService MetadataSync { get; } = Substitute.For<IProcedureMetadataSyncService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Server=127.0.0.1,1;Database=QueryPlus_Test;User Id=sa;Password=invalid;Connect Timeout=1;TrustServerCertificate=True");
        builder.UseSetting("Keycloak:Authority", "http://localhost:8080/realms/queryplus");
        builder.UseSetting("Keycloak:MetadataAddress",
            "http://127.0.0.1:1/realms/queryplus/.well-known/openid-configuration");
        builder.UseSetting("Keycloak:ClientId", "test-client");
        builder.UseSetting("Keycloak:ClientSecret", "test-secret");
        builder.UseSetting("Keycloak:RequireHttpsMetadata", "false");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "http://localhost:8080/realms/queryplus",
                ["Keycloak:MetadataAddress"] = "http://127.0.0.1:1/realms/queryplus/.well-known/openid-configuration",
                ["Keycloak:ClientId"] = "test-client",
                ["Keycloak:ClientSecret"] = "test-secret",
                ["Keycloak:RequireHttpsMetadata"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProcedureService>();
            services.RemoveAll<ICategoryService>();
            services.RemoveAll<IExecutionService>();
            services.RemoveAll<IExcelExportService>();
            services.RemoveAll<IProcedureRepository>();
            services.RemoveAll<IProcedureMetadataSyncService>();

            services.AddSingleton(Procedures);
            services.AddSingleton(Categories);
            services.AddSingleton(Execution);
            services.AddSingleton(Exports);
            services.AddSingleton(ProcedureRepository);
            services.AddSingleton(MetadataSync);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = AnonymousAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = AnonymousAuthHandler.SchemeName;
                    options.DefaultScheme = AnonymousAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, AnonymousAuthHandler>(
                    AnonymousAuthHandler.SchemeName,
                    _ => { });

            services.PostConfigureAll<OpenIdConnectOptions>(options =>
            {
                options.MetadataAddress = "http://127.0.0.1:1/realms/queryplus/.well-known/openid-configuration";
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                    new OpenIdConnectConfiguration
                    {
                        Issuer = "http://localhost:8080/realms/queryplus",
                        AuthorizationEndpoint = "http://localhost:8080/realms/queryplus/protocol/openid-connect/auth",
                        TokenEndpoint = "http://localhost:8080/realms/queryplus/protocol/openid-connect/token",
                        UserInfoEndpoint = "http://localhost:8080/realms/queryplus/protocol/openid-connect/userinfo",
                        JwksUri = "http://localhost:8080/realms/queryplus/protocol/openid-connect/certs",
                        EndSessionEndpoint = "http://localhost:8080/realms/queryplus/protocol/openid-connect/logout"
                    });
            });

            services.PostConfigureAll<CookieAuthenticationOptions>(options =>
            {
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
            });
        });
    }
}
