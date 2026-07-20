using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Interfaces;
using NSubstitute;

namespace QueryPlus.Web.Tests.Infrastructure;

/// <summary>
/// Boots the web host with test authentication and substituted application services.
/// Startup seed may fail against a dummy connection string (Development swallows it).
/// </summary>
public sealed class QueryPlusWebApplicationFactory : WebApplicationFactory<Program>
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

        builder.ConfigureTestServices(services =>
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

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }
}
