using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Mapping;
using QueryPlus.Application.Services;

namespace QueryPlus.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<QueryPlusMappingProfile>());
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        // Web should replace this with a claims-based implementation.
        services.AddScoped<ICurrentUserContext, SystemUserContext>();

        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProcedureService, ProcedureService>();
        services.AddScoped<IExecutionService, ExecutionService>();

        return services;
    }
}
