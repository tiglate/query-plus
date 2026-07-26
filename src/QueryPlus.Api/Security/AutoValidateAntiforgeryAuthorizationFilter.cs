using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QueryPlus.Api.Security;

public sealed class AutoValidateAntiforgeryAuthorizationFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (!UnsafeMethods.Contains(method))
        {
            return;
        }

        if (context.HttpContext.Request.Path.StartsWithSegments("/api/auth/csrf"))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException ex)
        {
            context.Result = new BadRequestObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "Antiforgery token validation failed",
                status = 400,
                detail = ex.Message,
                instance = context.HttpContext.Request.Path
            });
        }
    }
}
