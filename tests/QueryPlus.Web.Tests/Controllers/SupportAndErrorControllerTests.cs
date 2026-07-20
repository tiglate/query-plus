using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NSubstitute;
using QueryPlus.Web.Controllers;
using QueryPlus.Web.ViewModels;

namespace QueryPlus.Web.Tests.Controllers;

public sealed class SupportAndErrorControllerTests
{
    [Fact]
    public void Support_Index_returns_view()
    {
        var result = new SupportController().Index();
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Privacy_Index_returns_view()
    {
        var result = new PrivacyController().Index();
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Error_Index_uses_status_code_query()
    {
        var controller = new ErrorController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Index(statusCode: 404);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ErrorViewModel>().Subject;
        model.StatusCodeValue.Should().Be(404);
        model.IsNotFound.Should().BeTrue();
        controller.HttpContext.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public void Culture_Set_redirects_to_local_return_url()
    {
        var urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.IsLocalUrl("/Support").Returns(true);

        var controller = new CultureController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            },
            Url = urlHelper
        };

        var result = controller.Set("en", "/Support");

        result.Should().BeOfType<LocalRedirectResult>()
            .Which.Url.Should().Be("/Support");
    }

    [Fact]
    public void Culture_Set_rejects_external_return_url()
    {
        var urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.IsLocalUrl("https://evil.example").Returns(false);

        var controller = new CultureController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            },
            Url = urlHelper
        };

        var result = controller.Set("en", "https://evil.example");

        result.Should().BeOfType<LocalRedirectResult>()
            .Which.Url.Should().Be("/");
    }
}
