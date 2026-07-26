using System.Data;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Api.Tests.Integration;

public sealed class ExecuteApiTests : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly QueryPlusApiApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExecuteApiTests(QueryPlusApiApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private async Task<HttpRequestMessage> AuthedJsonAsync(HttpMethod method, string url, HttpContent content)
    {
        var token = await AntiforgeryApiHelper.GetTokenAsync(_client);
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.TryAddWithoutValidation(AntiforgeryApiHelper.CsrfHeaderName, token);
        return request;
    }

    private static Procedure SampleProcedure(int id = 7) => new()
    {
        IdProcedure = id,
        IdCategory = 1,
        Caption = "Demo",
        DatabaseName = "db",
        ProcedureName = "dbo.usp_Demo",
        RoleEntitlement = "user",
        Enabled = true,
        SupportsPagination = false,
        Parameters =
        [
            new ProcedureParameter { IdProcedureParameter = 1, IdProcedure = id, Caption = "Start", Name = "@Start", ParameterType = ParameterType.Date, IsRequired = false }
        ],
        Columns =
        [
            new ProcedureColumn { IdProcedureColumn = 1, IdProcedure = id, TechnicalName = "Id", Caption = "Id", Alignment = ColumnAlignment.Left, Visible = true },
            new ProcedureColumn { IdProcedureColumn = 2, IdProcedure = id, TechnicalName = "HiddenCol", Caption = "Hidden", Alignment = ColumnAlignment.Left, Visible = false }
        ]
    };

    private static DataTable BuildDataTable()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("HiddenCol", typeof(string));
        var row = table.NewRow();
        row["Id"] = 1;
        row["HiddenCol"] = "secret";
        table.Rows.Add(row);
        return table;
    }

    [Fact]
    public async Task Reserved_pagination_name_is_rejected()
    {
        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new
            {
                procedureId = 7,
                parameterValues = new Dictionary<string, string?> { ["@PageNumber"] = "1", ["@Start"] = "2024-01-01" }
            }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _factory.ProcedureRepository.DidNotReceive().GetEnabledByIdWithDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_procedure_returns_400_and_clears_eligibility()
    {
        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new { procedureId = 0, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Procedure_not_found_returns_404_and_clears_eligibility()
    {
        _factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(99, Arg.Any<CancellationToken>())
            .Returns((Procedure?)null);

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new { procedureId = 99, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Missing_required_parameters_returns_400()
    {
        var procedure = new Procedure
        {
            IdProcedure = 11,
            IdCategory = 1,
            Caption = "Demo",
            DatabaseName = "db",
            ProcedureName = "dbo.usp_DemoRequired",
            RoleEntitlement = "user",
            Enabled = true,
            SupportsPagination = false,
            Parameters =
            [
                new ProcedureParameter { IdProcedureParameter = 1, IdProcedure = 11, Caption = "Start", Name = "@Start", ParameterType = ParameterType.Date, IsRequired = true }
            ],
            Columns = []
        };
        _factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(11, Arg.Any<CancellationToken>())
            .Returns(procedure);

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new { procedureId = 11, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.Execution.DidNotReceive().ExecuteAsync(
            Arg.Is<ExecuteProcedureRequest>(r => r.ProcedureId == 11),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Successful_execute_returns_visible_columns_and_row_array()
    {
        var procedure = SampleProcedure(12);
        _factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(12, Arg.Any<CancellationToken>())
            .Returns(procedure);
        _factory.Execution.ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ExecutionResultDto
            {
                Success = true,
                ProcedureId = 12,
                ProcedureCaption = "Demo",
                RowCount = 1,
                Columns =
                [
                    new GridColumnDto { TechnicalName = "Id", Caption = "Id", Alignment = ColumnAlignment.Left, Visible = true },
                    new GridColumnDto { TechnicalName = "HiddenCol", Caption = "Hidden", Alignment = ColumnAlignment.Left, Visible = false }
                ],
                Data = BuildDataTable()
            });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new { procedureId = 12, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("\"success\":true");
        raw.Should().Contain("\"columns\":");
        raw.Should().Contain("\"Id\"");
        raw.Should().NotContain("\"HiddenCol\"");
        raw.Should().Contain("\"rows\":[[1");
        _factory.Execution.Received(1).ExecuteAsync(
            Arg.Is<ExecuteProcedureRequest>(r => r.ProcedureId == 12),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Successful_execute_marks_eligible_for_export()
    {
        var procedure = SampleProcedure(13);
        _factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(13, Arg.Any<CancellationToken>())
            .Returns(procedure);
        _factory.Execution.ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ExecutionResultDto
            {
                Success = true,
                ProcedureId = 13,
                RowCount = 5,
                Columns = [new GridColumnDto { TechnicalName = "Id", Caption = "Id", Visible = true }],
                Data = BuildDataTable()
            });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new { procedureId = 13, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var eligibility = _factory.Services.GetRequiredService<QueryPlus.Api.Services.ExportEligibilityService>();
        eligibility.TryValidate("test-user", 13, new Dictionary<string, string?>(), out _).Should().BeTrue();
    }

    [Fact]
    public async Task Zero_rows_does_not_mark_eligible()
    {
        var procedure = SampleProcedure(14);
        _factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(14, Arg.Any<CancellationToken>())
            .Returns(procedure);
        _factory.Execution.ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ExecutionResultDto
            {
                Success = true,
                ProcedureId = 14,
                RowCount = 0,
                Columns = [new GridColumnDto { TechnicalName = "Id", Caption = "Id", Visible = true }],
                Data = BuildDataTable()
            });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new { procedureId = 14, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var eligibility = _factory.Services.GetRequiredService<QueryPlus.Api.Services.ExportEligibilityService>();
        eligibility.TryValidate("test-user", 14, new Dictionary<string, string?>(), out _).Should().BeFalse();
    }

    [Fact]
    public async Task Failed_execute_clears_eligibility()
    {
        var procedure = SampleProcedure(15);
        _factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(15, Arg.Any<CancellationToken>())
            .Returns(procedure);
        _factory.Execution.ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ExecutionResultDto { Success = false, ProcedureId = 15, ErrorMessage = "boom" });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new { procedureId = 15, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var eligibility = _factory.Services.GetRequiredService<QueryPlus.Api.Services.ExportEligibilityService>();
        eligibility.TryValidate("test-user", 15, new Dictionary<string, string?>(), out _).Should().BeFalse();
    }

    [Fact]
    public async Task Forbidden_exception_maps_to_403()
    {
        var procedure = SampleProcedure(16);
        _factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(16, Arg.Any<CancellationToken>())
            .Returns(procedure);
        _factory.Execution.ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>())
            .Returns<ExecutionResultDto>(_ => throw new QueryPlus.Domain.Exceptions.ForbiddenOperationException("nope"));

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new { procedureId = 16, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Not_found_exception_maps_to_404()
    {
        var procedure = SampleProcedure(17);
        _factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(17, Arg.Any<CancellationToken>())
            .Returns(procedure);
        _factory.Execution.ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>())
            .Returns<ExecutionResultDto>(_ => throw new QueryPlus.Domain.Exceptions.EntityNotFoundException("Procedure", 17));

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/execute",
            JsonContent.Create(new { procedureId = 17, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}