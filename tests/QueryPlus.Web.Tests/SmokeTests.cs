using FluentAssertions;
using QueryPlus.Web.Controllers;
using QueryPlus.Web.Controllers.Admin;

namespace QueryPlus.Web.Tests;

/// <summary>
/// Sanity checks that the MVC surface area is wired (controllers resolve / exist).
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Mvc_controllers_are_present()
    {
        typeof(HomeController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Select(t => t.Name)
            .Should()
            .Contain(
            [
                nameof(HomeController),
                nameof(AccountController),
                nameof(ExportsController),
                nameof(CategoriesController),
                nameof(ProceduresController),
                nameof(SupportController),
                nameof(ErrorController),
                nameof(CultureController)
            ]);
    }
}
