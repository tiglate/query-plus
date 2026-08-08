using Microsoft.Extensions.DependencyInjection.Extensions;
using QueryPlus.Api.Infrastructure;
using QueryPlus.Api.Services;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Api.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // Replace (not just add) AddApplication()'s SystemUserContext fallback: this makes the
        // override explicit in code rather than relying on "last registration wins" ordering
        // against whatever Program.cs happens to call AddApiServices() after.
        services.Replace(ServiceDescriptor.Scoped<ICurrentUserContext, HttpCurrentUserContext>());

        services.AddSingleton<ExcelExportService>();
        services.AddSingleton<IExcelExportService>(provider => provider.GetRequiredService<ExcelExportService>());
        services.AddSingleton<ExportEligibilityService>();
        services.AddHostedService<ExcelExportBackgroundService>();
        return services;
    }
}
