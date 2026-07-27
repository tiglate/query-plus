using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using QueryPlus.Api.Infrastructure;

namespace QueryPlus.Api.Tests;

public class HttpCurrentUserContextTests
{
    private readonly IHttpContextAccessor _accessor = Substitute.For<IHttpContextAccessor>();
    private readonly HttpCurrentUserContext _sut;

    public HttpCurrentUserContextTests()
    {
        _sut = new HttpCurrentUserContext(_accessor);
    }

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenNoHttpContextOrUser()
    {
        _accessor.HttpContext.Returns((HttpContext?)null);

        _sut.IsAuthenticated.Should().BeFalse();
        _sut.Username.Should().Be("anonymous");
        _sut.Roles.Should().BeEmpty();
        _sut.IpAddress.Should().BeNull();
    }

    [Fact]
    public void UserContext_ExtractsPreferredUsername_Roles_And_ForwardedIp()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "192.168.1.10, 10.0.0.1";

        var claims = new List<Claim>
        {
            new("preferred_username", "alice"),
            new("roles", "admin"),
            new(ClaimTypes.Role, "user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        _accessor.HttpContext.Returns(httpContext);

        _sut.IsAuthenticated.Should().BeTrue();
        _sut.Username.Should().Be("alice");
        _sut.IpAddress.Should().Be("192.168.1.10");
        _sut.Roles.Should().Contain("admin", "user");
        _sut.IsInRole("admin").Should().BeTrue();
    }
}
