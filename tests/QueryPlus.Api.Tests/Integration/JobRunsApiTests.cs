using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Exceptions;

namespace QueryPlus.Api.Tests.Integration;

public sealed class JobRunsApiTests(QueryPlusApiApplicationFactory factory)
    : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private static JobRunDetailDto SampleRun(int id, JobRunStatus status = JobRunStatus.Succeeded) => new()
    {
        Id = id,
        JobDefinitionId = 7,
        Status = status,
        TriggeredBy = JobTriggerSource.Schedule,
        StartedAt = DateTime.UtcNow.AddMinutes(-5),
        FinishedAt = DateTime.UtcNow,
        ExitCode = 0,
        HostMachine = "runner-01",
        CreatedAt = DateTime.UtcNow.AddMinutes(-5)
    };

    [Fact]
    public async Task Search_returns_paged_results()
    {
        factory.JobRuns.SearchAsync(Arg.Any<JobRunFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<JobRunListItemDto>
            {
                Items =
                [
                    new JobRunListItemDto
                    {
                        Id = 1, JobDefinitionId = 7, Status = JobRunStatus.Succeeded,
                        TriggeredBy = JobTriggerSource.Schedule
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });

        var response = await _client.GetAsync("/api/jobs/runs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<PagedResult<JobRunListItemDto>>();
        json.Should().NotBeNull();
        json.Items.Should().ContainSingle();
        await factory.JobRuns.Received(1).SearchAsync(
            Arg.Is<JobRunFilterDto>(f => f.Page == 1 && f.PageSize == 20), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_by_id_returns_detail()
    {
        factory.JobRuns.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(SampleRun(1));

        var response = await _client.GetAsync("/api/jobs/runs/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JobRunDetailDto>();
        json!.Id.Should().Be(1);
        json.JobDefinitionId.Should().Be(7);
    }

    [Fact]
    public async Task Get_by_id_missing_returns_404()
    {
        factory.JobRuns.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((JobRunDetailDto?)null);

        var response = await _client.GetAsync("/api/jobs/runs/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_run_request_returns_detail()
    {
        factory.JobRuns.GetRunRequestAsync(3, Arg.Any<CancellationToken>()).Returns(new JobRunRequestDto
        {
            Id = 3, JobDefinitionId = 7, RequestedBy = "analyst1", RequestedAt = DateTime.UtcNow
        });

        var response = await _client.GetAsync("/api/jobs/runs/requests/3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JobRunRequestDto>();
        json!.Id.Should().Be(3);
        json.JobDefinitionId.Should().Be(7);
    }

    [Fact]
    public async Task Get_run_request_missing_returns_404()
    {
        factory.JobRuns.GetRunRequestAsync(999, Arg.Any<CancellationToken>()).Returns((JobRunRequestDto?)null);

        var response = await _client.GetAsync("/api/jobs/runs/requests/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Logs_returns_stdout_as_plain_text()
    {
        factory.JobRuns.ReadLogAsync(1, JobLogStream.Stdout, Arg.Any<CancellationToken>())
            .Returns("line one\nline two\n");

        var response = await _client.GetAsync("/api/jobs/runs/1/logs/Stdout");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Be("line one\nline two\n");
    }

    [Fact]
    public async Task Logs_returns_stderr_as_plain_text()
    {
        factory.JobRuns.ReadLogAsync(1, JobLogStream.Stderr, Arg.Any<CancellationToken>())
            .Returns("boom\n");

        var response = await _client.GetAsync("/api/jobs/runs/1/logs/Stderr");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Be("boom\n");
    }

    [Fact]
    public async Task Logs_missing_run_propagates_to_global_404_handler()
    {
        factory.JobRuns.ReadLogAsync(999, JobLogStream.Stdout, Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new EntityNotFoundException(nameof(QueryPlus.Domain.Entities.JobRun), 999));

        var response = await _client.GetAsync("/api/jobs/runs/999/logs/Stdout");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
