using QueryPlus.Api.Infrastructure;
using QueryPlus.Api.Services;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Interfaces;
using QueryPlus.Data.Interceptors;

namespace QueryPlus.Api.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddScoped<IAuditContext, HttpAuditContext>();
        services.AddSingleton<ExcelExportService>();
        services.AddSingleton<IExcelExportService>(provider => provider.GetRequiredService<ExcelExportService>());
        services.AddSingleton<ExportEligibilityService>();
        services.AddHostedService<ExcelExportBackgroundService>();
        return services;
    }
}
