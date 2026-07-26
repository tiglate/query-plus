using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using QueryPlus.Api.Tests.Infrastructure;

namespace QueryPlus.Api.Tests.Integration;

public sealed class ReactLogoutTransportTests : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly QueryPlusApiApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReactLogoutTransportTests(QueryPlusApiApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Logout_with_form_urlencoded_antiforgery_enters_sign_out_redirect()
    {
        var token = await AntiforgeryApiHelper.GetTokenAsync(_client);

        using var request = AntiforgeryApiHelper.CreateFormPost("/api/auth/logout", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
        ((int)response.StatusCode).Should().BeOneOf(
            (int)HttpStatusCode.Redirect,
            (int)HttpStatusCode.Found,
            (int)HttpStatusCode.RedirectMethod,
            (int)HttpStatusCode.TemporaryRedirect);
    }
}