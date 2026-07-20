using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QueryPlus.Web.ViewModels;

namespace QueryPlus.Web.Controllers;

/// <summary>
/// Custom error page for 404, 500, and other status codes.
/// Used by <c>UseStatusCodePagesWithReExecute</c> and <c>UseExceptionHandler</c>.
/// </summary>
[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public sealed class ErrorController : Controller
{
    [HttpGet("/Error")]
    public IActionResult Index(int? statusCode = null)
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        int statusCodeValue;

        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature is not null)
        {
            statusCodeValue = 500;
        }
        else if (statusCode is >= 400 and < 600)
        {
            statusCodeValue = statusCode.Value;
        }
        else
        {
            var code = HttpContext.Response.StatusCode;
            statusCodeValue = code is >= 400 and < 600 ? code : 500;
        }

        HttpContext.Response.StatusCode = statusCodeValue;
        return View(new ErrorViewModel
        {
            StatusCodeValue = statusCodeValue,
            RequestId = requestId
        });
    }
}
