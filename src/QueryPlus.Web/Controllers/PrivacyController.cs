using Microsoft.AspNetCore.Mvc;

namespace QueryPlus.Web.Controllers;

public sealed class PrivacyController : Controller
{
    [HttpGet("/Privacy")]
    public IActionResult Index()
    {
        ViewData["PageKey"] = "privacy";
        return View();
    }
}
