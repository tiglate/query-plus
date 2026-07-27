using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Services;

using QueryPlus.Application.Services.Converters;

namespace QueryPlus.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        // Parameter Value Converters & Registry (OCP)
        services.AddSingleton<IParameterValueConverter, FreeTextValueConverter>();
        services.AddSingleton<IParameterValueConverter, ComboValueConverter>();
        services.AddSingleton<IParameterValueConverter, NumericValueConverter>();
        services.AddSingleton<IParameterValueConverter, DateValueConverter>();
        services.AddSingleton<IParameterValueConverter, TimeValueConverter>();
        services.AddSingleton<IParameterValueConverter, DateTimeValueConverter>();
        services.AddSingleton<IParameterValueConverter, BooleanValueConverter>();
        services.AddSingleton<IParameterConverterRegistry, ParameterConverterRegistry>();

        // Execution pipeline components (SRP)
        services.AddTransient<IGridColumnBuilder, GridColumnBuilder>();
        services.AddTransient<IExecutionParameterResolver, ExecutionParameterResolver>();

        // Web should replace this with a claims-based implementation.
        services.AddScoped<ICurrentUserContext, SystemUserContext>();

        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProcedureService, ProcedureService>();
        services.AddScoped<IExecutionService, ExecutionService>();

        return services;
    }
}
