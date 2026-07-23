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
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddScoped<IAuditContext, NullAuditContext>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProcedureRepository, ProcedureRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();
        services.AddScoped<IConfigurationAuditReader, ConfigurationAuditReader>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStoredProcedureExecutor, DapperStoredProcedureExecutor>();
        services.AddScoped<IProcedureMetadataSyncService, SqlProcedureMetadataSyncService>();

        // Generic repository kept for simple lookups if needed.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<DemoDataSeeder>();

        return services;
    }
}
