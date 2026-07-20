using Microsoft.AspNetCore.Mvc;

namespace QueryPlus.Web.Controllers;

public sealed class SupportController : Controller
{
    [HttpGet("/Support")]
    public IActionResult Index()
    {
        ViewData["PageKey"] = "support";
        return View();
    }
}
