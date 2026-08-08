using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using QueryPlus.Api.Services;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Api.Tests.Integration;

public sealed class ExportsApiTests(QueryPlusApiApplicationFactory factory)
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

    private static ProcedureDetailDto EnabledDetail(int id) => new()
    {
        Id = id,
        CategoryId = 1,
        Caption = "Demo",
        ConnectionName = "DefaultConnection",
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
        factory.Exports.DidNotReceive()
            .QueueExport(Arg.Any<int>(), Arg.Any<Dictionary<string, string?>>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<string>>());
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
        var eligibility = factory.Services.GetRequiredService<ExportEligibilityService>();
        eligibility.MarkEligible("test-user", 7, new Dictionary<string, string?>(), 5);
        factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(EnabledDetail(7));
        var jobId = Guid.NewGuid();
        factory.Exports.QueueExport(7, Arg.Any<IDictionary<string, string?>>(), "test-user",
            Arg.Any<IReadOnlyCollection<string>>()).Returns(jobId);
        factory.Exports.GetJob(jobId).Returns(new ExportJobDto
        {
            Id = jobId,
            Status = ExportJobStatus.Queued,
            Username = "test-user",
            CreatedAt = DateTime.UtcNow
        });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/exports",
            JsonContent.Create(new { procedureId = 7, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.Location.Should().NotBeNull();
        // Regression guard: the queue response body must use the same DTO shape (an "id" field)
        // as the status/download endpoints - a hand-built anonymous object here once used
        // "jobId" instead, which the SPA's ExportJob type never matched, silently breaking the
        // Export button (queueExport().then(job => setJobId(job.id)) received undefined).
        var body = await response.Content.ReadFromJsonAsync<ExportJobDto>();
        body!.Id.Should().Be(jobId);
        body.Status.Should().Be(ExportJobStatus.Queued);
        factory.Exports.Received(1).QueueExport(7, Arg.Any<IDictionary<string, string?>>(), "test-user",
            Arg.Any<IReadOnlyCollection<string>>());
    }

    [Fact]
    public async Task Queue_for_disabled_procedure_returns_404()
    {
        var eligibility = factory.Services.GetRequiredService<ExportEligibilityService>();
        eligibility.MarkEligible("test-user", 7, new Dictionary<string, string?>(), 5);
        factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new ProcedureDetailDto
        {
            Id = 7,
            CategoryId = 1,
            Caption = "Demo",
            ConnectionName = "DefaultConnection",
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
        factory.Exports.GetJob(jobId).Returns(new ExportJobDto
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
        factory.Exports.GetJob(jobId).Returns((ExportJobDto?)null);

        var response = await _client.GetAsync($"/api/exports/{jobId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Status_for_other_user_job_returns_404()
    {
        var jobId = Guid.NewGuid();
        factory.Exports.GetJob(jobId).Returns(new ExportJobDto
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
        factory.Exports.GetJob(jobId).Returns(new ExportJobDto
        {
            Id = jobId,
            Status = ExportJobStatus.Completed,
            Username = "test-user",
            FileName = "demo.xlsx"
        });
        factory.Exports.GetFilePath(jobId).Returns((string?)null);

        var response = await _client.GetAsync($"/api/exports/{jobId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
