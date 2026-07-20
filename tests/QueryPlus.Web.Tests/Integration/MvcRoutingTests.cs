using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Web.Tests.Infrastructure;

namespace QueryPlus.Web.Tests.Integration;

public sealed class MvcRoutingTests : IClassFixture<QueryPlusWebApplicationFactory>
{
    private readonly QueryPlusWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MvcRoutingTests(QueryPlusWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _factory.Procedures.GetAccessibleForCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _factory.Categories.SearchAsync(Arg.Any<CategoryFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryListItemDto>
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            });
        _factory.Categories.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _factory.Procedures.SearchAsync(Arg.Any<ProcedureFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProcedureListItemDto>
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home")]
    [InlineData("/Support")]
    [InlineData("/Privacy")]
    [InlineData("/Admin/Categories")]
    [InlineData("/Admin/Categories/Create")]
    [InlineData("/Admin/Procedures")]
    [InlineData("/Admin/Procedures/Create")]
    [InlineData("/Account/AccessDenied")]
    public async Task Authenticated_get_routes_return_success(string url)
    {
        var response = await _client.GetAsync(url);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        // Views should render (not redirect to login)
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task Home_parameters_without_id_returns_html_snippet()
    {
        var response = await _client.GetAsync("/Home/Parameters");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
        (body.Contains("Selecione", StringComparison.OrdinalIgnoreCase) || body.Contains("procedure", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task Category_details_missing_returns_not_found()
    {
        _factory.Categories.GetByIdAsync(999, Arg.Any<CancellationToken>())
            .Returns((CategoryDetailDto?)null);

        var response = await _client.GetAsync("/Admin/Categories/Details/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Category_details_found_returns_ok()
    {
        _factory.Categories.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto
            {
                Id = 1,
                Description = "Sales",
                CreatedAt = DateTime.UtcNow
            });

        var response = await _client.GetAsync("/Admin/Categories/Details/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Sales");
    }

    [Fact]
    public async Task Error_page_is_anonymous_and_returns_status()
    {
        var response = await _client.GetAsync("/Error?statusCode=404");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Export_download_missing_returns_not_found()
    {
        var jobId = Guid.NewGuid();
        _factory.Exports.GetFilePath(jobId).Returns((string?)null);

        var response = await _client.GetAsync($"/exports/download/{jobId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
