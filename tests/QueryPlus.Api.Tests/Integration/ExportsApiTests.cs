using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Api.Tests.Integration;

public sealed class ExportsApiTests : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly QueryPlusApiApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExportsApiTests(QueryPlusApiApplicationFactory factory)
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

    private static ProcedureDetailDto EnabledDetail(int id) => new()
    {
        Id = id,
        CategoryId = 1,
        Caption = "Demo",
        DatabaseName = "db",
        ProcedureName = "dbo.usp_Demo",
        Enabled = true,
        RoleEntitlement = "user"
    };

    [Fact]
    public async Task Queue_without_prior_execute_returns_400()
    {
        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/exports",
            JsonContent.Create(new { procedureId = 7, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.Exports.DidNotReceive().QueueExport(Arg.Any<int>(), Arg.Any<Dictionary<string, string?>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Queue_with_reserved_param_returns_400()
    {
        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/exports",
            JsonContent.Create(new
            {
                procedureId = 7,
                parameterValues = new Dictionary<string, string?> { ["@PageSize"] = "50" }
            }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Queue_after_eligible_execute_returns_202_with_job_id()
    {
        var eligibility = _factory.Services.GetRequiredService<QueryPlus.Api.Services.ExportEligibilityService>();
        eligibility.MarkEligible("test-user", 7, new Dictionary<string, string?>(), 5);
        _factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(EnabledDetail(7));
        var jobId = Guid.NewGuid();
        _factory.Exports.QueueExport(7, Arg.Any<IDictionary<string, string?>>(), "test-user").Returns(jobId);

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/exports",
            JsonContent.Create(new { procedureId = 7, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.Location.Should().NotBeNull();
        _factory.Exports.Received(1).QueueExport(7, Arg.Any<IDictionary<string, string?>>(), "test-user");
    }

    [Fact]
    public async Task Queue_for_disabled_procedure_returns_404()
    {
        var eligibility = _factory.Services.GetRequiredService<QueryPlus.Api.Services.ExportEligibilityService>();
        eligibility.MarkEligible("test-user", 7, new Dictionary<string, string?>(), 5);
        _factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new ProcedureDetailDto
        {
            Id = 7,
            CategoryId = 1,
            Caption = "Demo",
            DatabaseName = "db",
            ProcedureName = "dbo.usp_Demo",
            Enabled = false,
            RoleEntitlement = "user"
        });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/exports",
            JsonContent.Create(new { procedureId = 7, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Status_for_owned_job_returns_200()
    {
        var jobId = Guid.NewGuid();
        _factory.Exports.GetJob(jobId).Returns(new ExportJobDto
        {
            Id = jobId,
            Status = ExportJobStatus.Completed,
            Username = "test-user",
            FileName = "demo.xlsx",
            RowCount = 10,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });

        var response = await _client.GetAsync($"/api/exports/{jobId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<ExportJobDto>();
        json!.Status.Should().Be(ExportJobStatus.Completed);
    }

    [Fact]
    public async Task Status_for_missing_job_returns_404()
    {
        var jobId = Guid.NewGuid();
        _factory.Exports.GetJob(jobId).Returns((ExportJobDto?)null);

        var response = await _client.GetAsync($"/api/exports/{jobId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Status_for_other_user_job_returns_404()
    {
        var jobId = Guid.NewGuid();
        _factory.Exports.GetJob(jobId).Returns(new ExportJobDto
        {
            Id = jobId,
            Status = ExportJobStatus.Completed,
            Username = "other-user"
        });

        var response = await _client.GetAsync($"/api/exports/{jobId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_when_file_path_missing_returns_404()
    {
        var jobId = Guid.NewGuid();
        _factory.Exports.GetJob(jobId).Returns(new ExportJobDto
        {
            Id = jobId,
            Status = ExportJobStatus.Completed,
            Username = "test-user",
            FileName = "demo.xlsx"
        });
        _factory.Exports.GetFilePath(jobId).Returns((string?)null);

        var response = await _client.GetAsync($"/api/exports/{jobId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}