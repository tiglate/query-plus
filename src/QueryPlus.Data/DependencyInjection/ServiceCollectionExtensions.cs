using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Application.Interfaces;
using QueryPlus.Data.Context;
using QueryPlus.Data.Repositories;
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

        services.AddDbContext<QueryPlusDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStoredProcedureExecutor, DapperStoredProcedureExecutor>();

        return services;
    }
}
