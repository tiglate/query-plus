using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using QueryPlus.Web.Infrastructure;

namespace QueryPlus.Web.DependencyInjection;

/// <summary>
/// Traditional MVC: controllers + Razor views (no Razor Pages).
/// </summary>
public static class MvcServiceCollectionExtensions
{
    public static IServiceCollection AddWebMvc(this IServiceCollection services)
    {
        services
            .AddControllersWithViews(options =>
            {
                // App is private by default; opt out with [AllowAnonymous].
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));

                // Trim all form/query/route-bound strings globally.
                options.ModelBinderProviders.Insert(0, new TrimStringModelBinderProvider());
            });

        return services;
    }
}
