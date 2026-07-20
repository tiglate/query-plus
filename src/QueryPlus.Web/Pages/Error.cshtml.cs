using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueryPlus.Web.Pages;

/// <summary>
/// Custom error page for 404, 500, and other status codes (incl. 505).
/// Used by <c>UseStatusCodePagesWithReExecute</c> and <c>UseExceptionHandler</c>.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public int StatusCodeValue { get; private set; } = 500;

    public string? RequestId { get; private set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public bool IsNotFound => StatusCodeValue == 404;

    public bool IsServerError => StatusCodeValue is >= 500 and <= 599;

    public void OnGet(int? statusCode = null)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        // Exception handler path (no statusCode query) → 500.
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature is not null)
        {
            StatusCodeValue = 500;
        }
        else if (statusCode is >= 400 and < 600)
        {
            StatusCodeValue = statusCode.Value;
        }
        else
        {
            // Re-execute may set the status on the response already.
            var code = HttpContext.Response.StatusCode;
            StatusCodeValue = code is >= 400 and < 600 ? code : 500;
        }

        // Ensure the client still receives the correct HTTP status.
        HttpContext.Response.StatusCode = StatusCodeValue;
    }
}
