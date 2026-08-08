using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace QueryPlus.Api.DependencyInjection;

/// <summary>
/// Per-user concurrency limits for the endpoints that trigger long-running SQL work
/// (stored-procedure execution, Excel export), so a single user can't exhaust SQL Server
/// connections/worker threads by firing many overlapping requests.
/// </summary>
public static class RateLimitingServiceCollectionExtensions
{
    public const string ExecutePolicy = "execute";
    public const string ExportPolicy = "export";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Execute requests stay open for as long as the stored procedure runs (up to
            // ProcedurePagination.CommandTimeoutSeconds), so concurrency is the right shape:
            // it caps how many long-running executions one user can hold open at once.
            options.AddPolicy(ExecutePolicy, context => RateLimitPartition.GetConcurrencyLimiter(
                PartitionKey(context),
                _ => new ConcurrencyLimiterOptions { PermitLimit = 3, QueueLimit = 0 }));

            // Queueing an export returns almost immediately (it just enqueues work for the
            // single-consumer background worker), so the risk is queue depth over time, not
            // concurrent requests - a rate window bounds that instead.
            options.AddPolicy(ExportPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
        });

        return services;
    }

    private static string PartitionKey(HttpContext context) =>
        context.User.FindFirst("preferred_username")?.Value
        ?? context.User.Identity?.Name
        ?? "anonymous";
}
