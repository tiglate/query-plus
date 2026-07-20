using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QueryPlus.Web.Controllers;

/// <summary>
/// Authentication endpoints (OIDC challenge / sign-out) and access-denied UI.
/// </summary>
public sealed class AccountController : Controller
{
    /// <summary>Starts the Keycloak OIDC login flow.</summary>
    [HttpGet("/Account/Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        var redirect = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return Challenge(
            new AuthenticationProperties { RedirectUri = redirect },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Signs out of the app cookie and the OIDC session.
    /// Antiforgery is ignored to match the header form (no token); tighten later if desired.
    /// </summary>
    [HttpPost("/Account/Logout")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        return new EmptyResult();
    }

    [HttpGet("/Account/AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        ViewData["PageKey"] = "access-denied";
        return View();
    }
}
