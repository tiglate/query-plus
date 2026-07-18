using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Services;

namespace QueryPlus.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryDefinitionService, QueryDefinitionService>();
        return services;
    }
}
