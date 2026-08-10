using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Interfaces;
using QueryPlus.Data.DependencyInjection;
using QueryPlus.Infrastructure.Jobs;
using QueryPlus.Infrastructure.Notifications;

namespace QueryPlus.Infrastructure.DependencyInjection;

/// <summary>
/// Composition root for infrastructure concerns.
/// Registers Data layer and other external integrations.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddData(configuration);

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<INotificationSender, SmtpNotificationSender>();

        services.AddScoped<IJobRunAsUserCatalog, LinuxRunAsUserCatalog>();

        return services;
    }
}
