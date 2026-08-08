using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;

namespace QueryPlus.Api.Tests.Integration;

public sealed class CategoriesApiTests(QueryPlusApiApplicationFactory factory)
    : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task<HttpRequestMessage> AuthedJsonAsync(HttpMethod method, string url, HttpContent content)
    {
        var token = await AntiforgeryApiHelper.GetTokenAsync(_client);
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.TryAddWithoutValidation(AntiforgeryApiHelper.CsrfHeaderName, token);
        return request;
    }

    [Fact]
    public async Task Get_returns_paged_list()
    {
        factory.Categories.SearchAsync(Arg.Any<CategoryFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryListItemDto>
            {
                Items =
                [
                    new CategoryListItemDto { Id = 1, Description = "Sales", CreatedAt = DateTime.UtcNow },
                    new CategoryListItemDto { Id = 2, Description = "Marketing", CreatedAt = DateTime.UtcNow }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 20
            });

        var response = await _client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<PagedResult<CategoryListItemDto>>();
        json.Should().NotBeNull();
        json.Items.Should().HaveCount(2);
        json.TotalCount.Should().Be(2);
        await factory.Categories.Received(1).SearchAsync(
            Arg.Is<CategoryFilterDto>(f => f.Page == 1 && f.PageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_lookup_returns_all()
    {
        factory.Categories.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([
                new CategoryListItemDto { Id = 1, Description = "Sales", CreatedAt = DateTime.UtcNow }
            ]);

        var response = await _client.GetAsync("/api/categories/lookup");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<List<CategoryListItemDto>>();
        json.Should().ContainSingle();
    }

    [Fact]
    public async Task Get_by_id_returns_detail()
    {
        factory.Categories.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto { Id = 7, Description = "Sales", CreatedAt = DateTime.UtcNow });

        var response = await _client.GetAsync("/api/categories/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<CategoryDetailDto>();
        json!.Id.Should().Be(7);
        json.Description.Should().Be("Sales");
    }

    [Fact]
    public async Task Get_by_id_missing_returns_404()
    {
        factory.Categories.GetByIdAsync(999, Arg.Any<CancellationToken>())
            .Returns((CategoryDetailDto?)null);

        var response = await _client.GetAsync("/api/categories/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_returns_201_with_location_and_body()
    {
        factory.Categories.CreateAsync(Arg.Any<CreateCategoryDto>(), Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto { Id = 42, Description = "New", CreatedAt = DateTime.UtcNow });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/categories",
            JsonContent.Create(new { description = "New" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var json = await response.Content.ReadFromJsonAsync<CategoryDetailDto>();
        json!.Id.Should().Be(42);
        json.Description.Should().Be("New");
        await factory.Categories.Received(1).CreateAsync(
            Arg.Is<CreateCategoryDto>(c => c.Description == "New"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_returns_200_with_body()
    {
        factory.Categories.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto { Id = 7, Description = "Sales", CreatedAt = DateTime.UtcNow });
        factory.Categories.UpdateAsync(Arg.Any<UpdateCategoryDto>(), Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto { Id = 7, Description = "SalesX", CreatedAt = DateTime.UtcNow });

        using var request = await AuthedJsonAsync(HttpMethod.Put, "/api/categories/7",
            JsonContent.Create(new { description = "SalesX" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<CategoryDetailDto>();
        json!.Description.Should().Be("SalesX");
        await factory.Categories.Received(1).UpdateAsync(
            Arg.Is<UpdateCategoryDto>(u => u.Id == 7 && u.Description == "SalesX"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_missing_returns_404()
    {
        factory.Categories.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((CategoryDetailDto?)null);

        using var request = await AuthedJsonAsync(HttpMethod.Put, "/api/categories/99",
            JsonContent.Create(new { description = "X" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await factory.Categories.DidNotReceive()
            .UpdateAsync(Arg.Any<UpdateCategoryDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_returns_204()
    {
        factory.Categories.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto { Id = 7, Description = "Sales", CreatedAt = DateTime.UtcNow });

        using var request = await AuthedJsonAsync(HttpMethod.Delete, "/api/categories/7", new StringContent(""));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await factory.Categories.Received(1).DeleteAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_missing_returns_404()
    {
        factory.Categories.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((CategoryDetailDto?)null);

        using var request = await AuthedJsonAsync(HttpMethod.Delete, "/api/categories/99", new StringContent(""));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await factory.Categories.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
