using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Interfaces;
using QueryPlus.Data.Interceptors;
using QueryPlus.Web.Infrastructure;
using QueryPlus.Web.Services;

namespace QueryPlus.Web.DependencyInjection;

/// <summary>
/// Web-layer DI: HTTP-bound adapters and presentation services.
/// </summary>
public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddScoped<IAuditContext, HttpAuditContext>();

        services.AddSingleton<ExcelExportService>();
        services.AddSingleton<IExcelExportService>(sp => sp.GetRequiredService<ExcelExportService>());
        services.AddSingleton<ExportEligibilityService>();
        services.AddSingleton<DatabaseConnectionDisplay>();
        services.AddHostedService<ExcelExportBackgroundService>();

        return services;
    }
}
