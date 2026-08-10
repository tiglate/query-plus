using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Api.Tests.Integration;

public sealed class JobDefinitionsApiTests(QueryPlusApiApplicationFactory factory)
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

    private static JobDefinitionDetailDto SampleDetail(int id, JobApprovalStatus status = JobApprovalStatus.Draft) =>
        new()
        {
            Id = id,
            Name = "Nightly cleanup",
            JobType = JobType.Shell,
            ScriptPath = "cleanup.sh",
            CronExpression = "0 2 * * *",
            RunAsUser = "svc-jobs",
            Enabled = false,
            ApprovalStatus = status,
            CreatedBy = "analyst1",
            CreatedAt = DateTime.UtcNow
        };

    [Fact]
    public async Task Search_returns_paged_results()
    {
        factory.JobDefinitions.SearchAsync(Arg.Any<JobDefinitionFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<JobDefinitionListItemDto>
            {
                Items =
                [
                    new JobDefinitionListItemDto
                    {
                        Id = 1, Name = "Nightly cleanup", JobType = JobType.Shell,
                        ApprovalStatus = JobApprovalStatus.Draft, Enabled = false, CronExpression = "0 2 * * *",
                        RunAsUser = "svc-jobs", CreatedBy = "analyst1"
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });

        var response = await _client.GetAsync("/api/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<PagedResult<JobDefinitionListItemDto>>();
        json.Should().NotBeNull();
        json.Items.Should().ContainSingle();
        json.TotalCount.Should().Be(1);
        await factory.JobDefinitions.Received(1).SearchAsync(
            Arg.Is<JobDefinitionFilterDto>(f => f.Page == 1 && f.PageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_by_id_returns_detail()
    {
        factory.JobDefinitions.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(SampleDetail(7));

        var response = await _client.GetAsync("/api/jobs/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JobDefinitionDetailDto>();
        json!.Id.Should().Be(7);
        json.Name.Should().Be("Nightly cleanup");
    }

    [Fact]
    public async Task Get_by_id_missing_returns_404()
    {
        factory.JobDefinitions.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((JobDefinitionDetailDto?)null);

        var response = await _client.GetAsync("/api/jobs/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_returns_201_with_location_and_body()
    {
        factory.JobDefinitions.CreateAsync(Arg.Any<CreateJobDefinitionDto>(), Arg.Any<CancellationToken>())
            .Returns(SampleDetail(42));

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/jobs", JsonContent.Create(new
        {
            name = "Nightly cleanup",
            jobType = JobType.Shell,
            scriptPath = "cleanup.sh",
            cronExpression = "0 2 * * *",
            runAsUser = "svc-jobs",
            memoryLimitMb = 512,
            maxDurationMinutes = 30
        }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var json = await response.Content.ReadFromJsonAsync<JobDefinitionDetailDto>();
        json!.Id.Should().Be(42);
        await factory.JobDefinitions.Received(1).CreateAsync(
            Arg.Is<CreateJobDefinitionDto>(c => c.Name == "Nightly cleanup"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_returns_200_with_body()
    {
        factory.JobDefinitions.UpdateAsync(7, Arg.Any<UpdateJobDefinitionDto>(), Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7));

        // Deliberately omits "id" from the body - the route id must win (mirrors
        // CategoriesController/ProceduresController), so a client following the normal REST
        // shape (id in the route only) doesn't trip UpdateJobDefinitionDtoValidator's Id rule.
        using var request = await AuthedJsonAsync(HttpMethod.Put, "/api/jobs/7", JsonContent.Create(new
        {
            name = "Nightly cleanup",
            jobType = JobType.Shell,
            scriptPath = "cleanup.sh",
            cronExpression = "0 2 * * *",
            runAsUser = "svc-jobs",
            memoryLimitMb = 512,
            maxDurationMinutes = 30
        }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JobDefinitionDetailDto>();
        json!.Id.Should().Be(7);
        await factory.JobDefinitions.Received(1).UpdateAsync(
            7, Arg.Is<UpdateJobDefinitionDto>(u => u.Id == 7 && u.Name == "Nightly cleanup"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_returns_204()
    {
        using var request = await AuthedJsonAsync(HttpMethod.Delete, "/api/jobs/7", new StringContent(""));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await factory.JobDefinitions.Received(1).DeleteAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_returns_200_with_pending_status()
    {
        factory.JobDefinitions.SubmitForApprovalAsync(7, Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7, JobApprovalStatus.PendingApproval));

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/jobs/7/submit", new StringContent(""));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JobDefinitionDetailDto>();
        json!.ApprovalStatus.Should().Be(JobApprovalStatus.PendingApproval);
    }

    [Fact]
    public async Task Approve_returns_200_with_approved_status()
    {
        factory.JobDefinitions.ApproveAsync(7, Arg.Any<ApproveJobDefinitionDto>(), Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7, JobApprovalStatus.Approved));

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/jobs/7/approve",
            JsonContent.Create(new { comment = "Looks good" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JobDefinitionDetailDto>();
        json!.ApprovalStatus.Should().Be(JobApprovalStatus.Approved);
        await factory.JobDefinitions.Received(1).ApproveAsync(
            7, Arg.Is<ApproveJobDefinitionDto>(a => a.Comment == "Looks good"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_returns_200_with_rejected_status()
    {
        factory.JobDefinitions.RejectAsync(7, Arg.Any<RejectJobDefinitionDto>(), Arg.Any<CancellationToken>())
            .Returns(SampleDetail(7, JobApprovalStatus.Rejected));

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/jobs/7/reject",
            JsonContent.Create(new { reason = "Missing tests" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JobDefinitionDetailDto>();
        json!.ApprovalStatus.Should().Be(JobApprovalStatus.Rejected);
        await factory.JobDefinitions.Received(1).RejectAsync(
            7, Arg.Is<RejectJobDefinitionDto>(r => r.Reason == "Missing tests"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEnabled_returns_200_with_updated_flag()
    {
        var enabledDetail = SampleDetail(7, JobApprovalStatus.Approved);
        factory.JobDefinitions.SetEnabledAsync(7, true, Arg.Any<CancellationToken>())
            .Returns(new JobDefinitionDetailDto
            {
                Id = enabledDetail.Id, Name = enabledDetail.Name, JobType = enabledDetail.JobType,
                ScriptPath = enabledDetail.ScriptPath, CronExpression = enabledDetail.CronExpression,
                RunAsUser = enabledDetail.RunAsUser, Enabled = true, ApprovalStatus = enabledDetail.ApprovalStatus,
                CreatedBy = enabledDetail.CreatedBy, CreatedAt = enabledDetail.CreatedAt
            });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/jobs/7/enabled",
            JsonContent.Create(true));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JobDefinitionDetailDto>();
        json!.Enabled.Should().BeTrue();
        await factory.JobDefinitions.Received(1).SetEnabledAsync(7, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunNow_returns_200_with_run_request()
    {
        factory.JobDefinitions.RequestRunNowAsync(7, Arg.Any<CancellationToken>())
            .Returns(new JobRunRequestDto
            {
                Id = 1, JobDefinitionId = 7, RequestedBy = "analyst1", RequestedAt = DateTime.UtcNow
            });

        using var request = await AuthedJsonAsync(HttpMethod.Post, "/api/jobs/7/run-now", new StringContent(""));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JobRunRequestDto>();
        json!.JobDefinitionId.Should().Be(7);
    }
}
