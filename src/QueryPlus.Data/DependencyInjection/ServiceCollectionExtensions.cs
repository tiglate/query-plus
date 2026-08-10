using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Interfaces;
using QueryPlus.Data.Context;
using QueryPlus.Data.Interceptors;
using QueryPlus.Data.Metadata;
using QueryPlus.Data.Repositories;
using QueryPlus.Data.Seed;
using QueryPlus.Data.StoredProcedures;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' is not configured.");

        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString,
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProcedureRepository, ProcedureRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();
        services.AddScoped<IJobDefinitionRepository, JobDefinitionRepository>();
        services.AddScoped<IJobRunRepository, JobRunRepository>();
        services.AddScoped<IJobRunRequestRepository, JobRunRequestRepository>();
        services.AddScoped<IConfigurationAuditReader, ConfigurationAuditReader>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStoredProcedureExecutor, DapperStoredProcedureExecutor>();
        services.AddScoped<IProcedureMetadataSyncService, SqlProcedureMetadataSyncService>();
        services.AddScoped<IProcedureConnectionCatalog, ProcedureConnectionCatalog>();

        services.AddScoped<DemoDataSeeder>();

        return services;
    }
}
