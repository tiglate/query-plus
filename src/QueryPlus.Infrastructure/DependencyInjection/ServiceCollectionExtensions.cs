using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Data.DependencyInjection;

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

        // Future: email, file storage, external APIs, etc.

        return services;
    }
}
