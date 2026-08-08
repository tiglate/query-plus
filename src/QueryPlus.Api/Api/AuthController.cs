using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryPlus.Application.Abstractions;

namespace QueryPlus.Api.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ICurrentUserContext user) : ControllerBase
{
    [HttpGet("user")]
    [AllowAnonymous]
    public IActionResult GetUser() => Ok(new { user.Username, user.Roles, user.IsAuthenticated });

    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf([FromServices] Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout() => SignOut(new AuthenticationProperties { RedirectUri = "/" },
        CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
}
