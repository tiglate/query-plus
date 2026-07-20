using FluentAssertions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QueryPlus.Web.Controllers;

namespace QueryPlus.Web.Tests.Controllers;

public sealed class AccountControllerTests
{
    [Fact]
    public void Login_returns_challenge_with_return_url()
    {
        var controller = new AccountController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Login("/Admin/Categories");

        var challenge = result.Should().BeOfType<ChallengeResult>().Subject;
        challenge.Properties!.RedirectUri.Should().Be("/Admin/Categories");
        challenge.AuthenticationSchemes.Should().Contain(OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Fact]
    public void Login_defaults_return_url_to_home()
    {
        var controller = new AccountController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Login(null);

        result.Should().BeOfType<ChallengeResult>()
            .Which.Properties!.RedirectUri.Should().Be("/");
    }

    [Fact]
    public void AccessDenied_returns_view()
    {
        var controller = new AccountController();
        var result = controller.AccessDenied();
        result.Should().BeOfType<ViewResult>();
    }
}
