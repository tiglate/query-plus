using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Api.Tests.Integration;

public sealed class ProceduresApiTests(QueryPlusApiApplicationFactory factory)
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

    private static ProcedureDetailDto SampleDetail(int id) => new()
    {
        Id = id,
        CategoryId = 1,
        Caption = "Demo",
        ConnectionName = "DefaultConnection",
        DatabaseName = "db",
        ProcedureName = "dbo.usp_Demo",
        Enabled = true,
        RoleEntitlement = "user",
        Parameters =
        [
            new ProcedureParameterDto
                { Id = 1, Caption = "Start", Name = "@Start", ParameterType = ParameterType.Date, IsRequired = false }
        ],
        Columns =
        [
            new ProcedureColumnDto
                { Id = 1, TechnicalName = "Id", Caption = "Id", Alignment = ColumnAlignment.Left, Visible = true },
            new ProcedureColumnDto
            {
                Id = 2, TechnicalName = "HiddenCol", Caption = "Hidden", Alignment = ColumnAlignment.Left,
                Visible = false
            }
        ]
    };

    [Fact]
    public async Task Accessible_returns_user_visible_procedures()
    {
        factory.Procedures.GetAccessibleForCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureLookupDto { Id = 1, CategoryId = 1, Caption = "Demo", RoleEntitlement = "user" }
            ]);

        var response = await _client.GetAsync("/api/procedures/accessible");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<List<ProcedureLookupDto>>();
        json.Should().ContainSingle();
    }

    [Fact]
    public async Task Search_returns_paged_results()
    {
        factory.Procedures.SearchAsync(Arg.Any<ProcedureFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProcedureListItemDto>
            {
                Items =
                [
                    new ProcedureListItemDto
                    {
                        Id = 1, CategoryId = 1, Caption = "Demo", ConnectionName = "DefaultConnection", DatabaseName = "db", ProcedureName = "dbo.usp_Demo",
                        RoleEntitlement = "user"
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });

        var response = await _client.GetAsync("/api/procedures?caption=Demo&enabled=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<PagedResult<ProcedureListItemDto>>();
        json!.Items.Should().ContainSingle();
        await factory.Procedures.Received(1).SearchAsync(
            Arg.Is<ProcedureFilterDto>(f => f.Caption == "Demo" && f.Enabled == true && f.Page == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lookup_returns_all()
    {
        factory.Procedures.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([new ProcedureLookupDto { Id = 1, CategoryId = 1, Caption = "Demo", RoleEntitlement = "user" }]);

        var response = await _client.GetAsync("/api/procedures/lookup");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<List<ProcedureLookupDto>>();
        json.Should().ContainSingle();
    }

    [Fact]
    public async Task Get_by_id_returns_detail()
    {
        factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7));

        var response = await _client.GetAsync("/api/procedures/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<ProcedureDetailDto>();
        json!.Id.Should().Be(7);
        json.Caption.Should().Be("Demo");
    }

    [Fact]
    public async Task Get_by_id_missing_returns_404()
    {
        factory.Procedures.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((ProcedureDetailDto?)null);

        var response = await _client.GetAsync("/api/procedures/99");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_parameters_returns_list()
    {
        factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7));

        var response = await _client.GetAsync("/api/procedures/7/parameters");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<List<ProcedureParameterDto>>();
        json!.Should().ContainSingle(p => p.Name == "@Start");
    }

    [Fact]
    public async Task Get_parameters_missing_returns_404()
    {
        factory.Procedures.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((ProcedureDetailDto?)null);

        var response = await _client.GetAsync("/api/procedures/99/parameters");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_returns_201_with_location()
    {
        factory.Procedures.CreateAsync(Arg.Any<SaveProcedureDto>(), Arg.Any<CancellationToken>())
            .Returns(SampleDetail(42));

        var dto = new
        {
            categoryId = 1,
            caption = "Demo",
            connectionName = "DefaultConnection",
            databaseName = "db",
            procedureName = "dbo.usp_Demo",
            enabled = true,
            supportsPagination = false,
            roleEntitlement = "user",
            description = (string?)null,
            parameters = Array.Empty<object>(),
            columns = Array.Empty<object>()
        };

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/procedures", JsonContent.Create(dto));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var json = await response.Content.ReadFromJsonAsync<ProcedureDetailDto>();
        json!.Id.Should().Be(42);
        await factory.Procedures.Received(1).CreateAsync(
            Arg.Is<SaveProcedureDto>(s => s.Caption == "Demo" && s.DatabaseName == "db"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_returns_200_with_body()
    {
        factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7));
        factory.Procedures.UpdateAsync(Arg.Any<SaveProcedureDto>(), Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7));

        var dto = new
        {
            categoryId = 1,
            caption = "Demo",
            connectionName = "DefaultConnection",
            databaseName = "db",
            procedureName = "dbo.usp_Demo",
            enabled = true,
            supportsPagination = false,
            roleEntitlement = "user",
            description = (string?)null,
            parameters = Array.Empty<object>(),
            columns = Array.Empty<object>()
        };

        using var request = await AuthedJsonAsync(HttpMethod.Put, "/api/procedures/7", JsonContent.Create(dto));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.Procedures.Received(1).UpdateAsync(
            Arg.Is<SaveProcedureDto>(s => s.Id == 7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_missing_returns_404()
    {
        factory.Procedures.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((ProcedureDetailDto?)null);

        var dto = new
        {
            categoryId = 1,
            caption = "Demo",
            connectionName = "DefaultConnection",
            databaseName = "db",
            procedureName = "dbo.usp_Demo",
            enabled = true,
            supportsPagination = false,
            roleEntitlement = "user",
            description = (string?)null,
            parameters = Array.Empty<object>(),
            columns = Array.Empty<object>()
        };

        using var request = await AuthedJsonAsync(HttpMethod.Put, "/api/procedures/99", JsonContent.Create(dto));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_returns_204()
    {
        factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7));

        using var request = await AuthedJsonAsync(HttpMethod.Delete, "/api/procedures/7", new StringContent(""));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await factory.Procedures.Received(1).DeleteAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sync_metadata_returns_snapshot_for_existing_procedure()
    {
        factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7));
        factory.MetadataSync.FetchAsync("DefaultConnection", "dbExisting", "dbo.usp_Existing", Arg.Any<CancellationToken>())
            .Returns(new ProcedureMetadataSnapshot
            {
                Parameters = [new SaveProcedureParameterDto { Caption = "Start", Name = "@Start" }],
                Columns = [new SaveProcedureColumnDto { TechnicalName = "Id", Caption = "Id" }]
            });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/procedures/7/sync-metadata",
            JsonContent.Create(new { connectionName = "DefaultConnection", databaseName = "dbExisting", procedureName = "dbo.usp_Existing" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<ProcedureMetadataSnapshot>();
        json!.Parameters.Should().ContainSingle();
        json.Columns.Should().ContainSingle();
        await factory.MetadataSync.Received(1)
            .FetchAsync("DefaultConnection", "dbExisting", "dbo.usp_Existing", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sync_metadata_with_id_zero_works_without_existing_procedure()
    {
        factory.MetadataSync.FetchAsync("DefaultConnection", "dbNew", "dbo.usp_New", Arg.Any<CancellationToken>())
            .Returns(new ProcedureMetadataSnapshot());

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/procedures/0/sync-metadata",
            JsonContent.Create(new { connectionName = "DefaultConnection", databaseName = "dbNew", procedureName = "dbo.usp_New" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.MetadataSync.Received(1).FetchAsync("DefaultConnection", "dbNew", "dbo.usp_New", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sync_metadata_with_id_nonzero_missing_returns_404()
    {
        factory.Procedures.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((ProcedureDetailDto?)null);

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/procedures/99/sync-metadata",
            JsonContent.Create(new { connectionName = "DefaultConnection", databaseName = "db", procedureName = "dbo.usp_Demo" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Sync_metadata_with_missing_names_returns_400()
    {
        factory.Procedures.GetByIdAsync(0, Arg.Any<CancellationToken>())
            .Returns((ProcedureDetailDto?)null);

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/procedures/0/sync-metadata",
            JsonContent.Create(new { databaseName = "", procedureName = "" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
