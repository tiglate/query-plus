using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Api.Tests.Integration;

/// <summary>
/// Regression coverage for the role-gated catalog/execute/audit endpoints: each role
/// (ROLE_CATEGORY_READ/WRITE, ROLE_PROCEDURE_READ/WRITE, ROLE_QUERY_EXEC, ROLE_ADMIN) must
/// grant exactly what it claims and nothing more, except ROLE_ADMIN which grants everything.
/// </summary>
public sealed class AdminAuthorizationTests(QueryPlusApiApplicationFactory factory)
    : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private static HttpRequestMessage WithRoles(HttpMethod method, string url, string roles,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.TryAddWithoutValidation(TestAuthHandler.RolesHeader, roles);
        return request;
    }

    private async Task<HttpRequestMessage> WithRolesAndCsrfAsync(HttpMethod method, string url, string roles,
        HttpContent? content = null)
    {
        var token = await AntiforgeryApiHelper.GetTokenAsync(_client);
        var request = WithRoles(method, url, roles, content);
        request.Headers.TryAddWithoutValidation(AntiforgeryApiHelper.CsrfHeaderName, token);
        return request;
    }

    // ---- ROLE_QUERY_EXEC: baseline execute/browse permission, no catalog admin access ----

    [Fact]
    public async Task QueryExec_can_browse_accessible_procedures()
    {
        factory.Procedures.GetAccessibleForCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ProcedureLookupDto>());

        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/procedures/accessible",
            "ROLE_QUERY_EXEC"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task QueryExec_cannot_search_procedure_catalog()
    {
        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/procedures", "ROLE_QUERY_EXEC"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task QueryExec_cannot_search_categories()
    {
        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/categories", "ROLE_QUERY_EXEC"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task QueryExec_cannot_view_execution_logs()
    {
        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/execution-logs", "ROLE_QUERY_EXEC"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NoRoles_cannot_execute()
    {
        // "NONE" is a sentinel that maps to zero recognized roles - an empty header value gets
        // dropped by HttpClient before it reaches the server, which would fall back to the
        // handler's admin default and defeat the point of this test.
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/execute", "NONE",
            JsonContent.Create(new { procedureId = 1, parameterValues = new Dictionary<string, string?>() }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- ROLE_PROCEDURE_READ: catalog browsing only, no writes ----

    [Fact]
    public async Task ProcedureRead_can_search_procedure_catalog()
    {
        factory.Procedures.SearchAsync(Arg.Any<ProcedureFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProcedureListItemDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 });

        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/procedures", "ROLE_PROCEDURE_READ"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcedureRead_cannot_create_procedure()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/procedures", "ROLE_PROCEDURE_READ",
            JsonContent.Create(new { }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProcedureRead_cannot_delete_procedure()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Delete, "/api/procedures/1",
            "ROLE_PROCEDURE_READ");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProcedureRead_cannot_sync_metadata()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/procedures/0/sync-metadata",
            "ROLE_PROCEDURE_READ", JsonContent.Create(new { databaseName = "db", procedureName = "proc" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- ROLE_PROCEDURE_WRITE: full procedure CRUD ----

    [Fact]
    public async Task ProcedureWrite_can_delete_procedure()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Delete, "/api/procedures/1",
            "ROLE_PROCEDURE_WRITE");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // no such procedure in the mock, but not 403
    }

    // ---- ROLE_CATEGORY_READ / ROLE_CATEGORY_WRITE ----

    [Fact]
    public async Task CategoryRead_can_search_categories()
    {
        factory.Categories.SearchAsync(Arg.Any<CategoryFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryListItemDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 });

        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/categories", "ROLE_CATEGORY_READ"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CategoryRead_cannot_create_category()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/categories", "ROLE_CATEGORY_READ",
            JsonContent.Create(new { description = "Test" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProcedureWrite_can_read_category_lookup_for_the_procedure_editor()
    {
        factory.Categories.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<CategoryListItemDto>());

        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/categories/lookup",
            "ROLE_PROCEDURE_WRITE"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- ROLE_JOB_READ: job browsing only, no writes ----

    [Fact]
    public async Task JobRead_can_search_job_definitions()
    {
        factory.JobDefinitions.SearchAsync(Arg.Any<JobDefinitionFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<JobDefinitionListItemDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 });

        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/jobs", "ROLE_JOB_READ"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JobRead_can_search_job_runs()
    {
        factory.JobRuns.SearchAsync(Arg.Any<JobRunFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<JobRunListItemDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 });

        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/jobs/runs", "ROLE_JOB_READ"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JobRead_cannot_create_job_definition()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs", "ROLE_JOB_READ",
            JsonContent.Create(new { }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task JobRead_cannot_approve_job_definition()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs/1/approve", "ROLE_JOB_READ",
            JsonContent.Create(new { }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- ROLE_JOB_WRITE: create/edit/submit/enable/run-now, but not approve/reject ----
    // (segregation of duties: the same role that proposes a job must not be able to approve it)

    [Fact]
    public async Task JobWrite_can_create_job_definition()
    {
        factory.JobDefinitions.CreateAsync(Arg.Any<CreateJobDefinitionDto>(), Arg.Any<CancellationToken>())
            .Returns(new JobDefinitionDetailDto
            {
                Id = 1, Name = "Job", ScriptPath = "job.sh", CronExpression = "* * * * *", RunAsUser = "svc",
                CreatedBy = "creator", ApprovalStatus = JobApprovalStatus.Draft, CreatedAt = DateTime.UtcNow
            });

        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs", "ROLE_JOB_WRITE",
            JsonContent.Create(new { name = "Job", scriptPath = "job.sh", cronExpression = "* * * * *",
                runAsUser = "svc" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task JobWrite_can_submit_job_definition_for_approval()
    {
        factory.JobDefinitions.SubmitForApprovalAsync(1, Arg.Any<CancellationToken>())
            .Returns(new JobDefinitionDetailDto
            {
                Id = 1, Name = "Job", ScriptPath = "job.sh", CronExpression = "* * * * *", RunAsUser = "svc",
                CreatedBy = "creator", ApprovalStatus = JobApprovalStatus.PendingApproval,
                CreatedAt = DateTime.UtcNow
            });

        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs/1/submit", "ROLE_JOB_WRITE");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JobWrite_cannot_approve_job_definition()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs/1/approve", "ROLE_JOB_WRITE",
            JsonContent.Create(new { }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task JobWrite_cannot_reject_job_definition()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs/1/reject", "ROLE_JOB_WRITE",
            JsonContent.Create(new { reason = "not needed" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task JobWrite_can_upload_script()
    {
        factory.JobDefinitions.UploadScriptAsync(
                1, Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new JobDefinitionDetailDto
            {
                Id = 1, Name = "Job", ScriptPath = "/allowlist/uploads/job-1/run.sh", CronExpression = "* * * * *",
                RunAsUser = "svc", CreatedBy = "creator", ApprovalStatus = JobApprovalStatus.Draft,
                CreatedAt = DateTime.UtcNow
            });

        using var fileContent = new ByteArrayContent("#!/bin/bash\necho hi\n"u8.ToArray());
        using var form = new MultipartFormDataContent { { fileContent, "file", "backup.sh" } };

        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs/1/script", "ROLE_JOB_WRITE", form);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.JobDefinitions.Received(1).UploadScriptAsync(
            1, Arg.Any<Stream>(), "backup.sh", Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JobRead_cannot_upload_script()
    {
        using var fileContent = new ByteArrayContent("#!/bin/bash\necho hi\n"u8.ToArray());
        using var form = new MultipartFormDataContent { { fileContent, "file", "backup.sh" } };

        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs/1/script", "ROLE_JOB_READ", form);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task JobWrite_can_list_run_as_users()
    {
        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/jobs/run-as-users", "ROLE_JOB_WRITE"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JobRead_cannot_list_run_as_users()
    {
        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/jobs/run-as-users", "ROLE_JOB_READ"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- ROLE_JOB_APPROVE: approve/reject only, no create/edit ----
    // (segregation of duties: the approver role must not also be able to author job definitions)

    [Fact]
    public async Task JobApprove_can_approve_job_definition()
    {
        factory.JobDefinitions.ApproveAsync(1, Arg.Any<ApproveJobDefinitionDto>(), Arg.Any<CancellationToken>())
            .Returns(new JobDefinitionDetailDto
            {
                Id = 1, Name = "Job", ScriptPath = "job.sh", CronExpression = "* * * * *", RunAsUser = "svc",
                CreatedBy = "creator", ApprovalStatus = JobApprovalStatus.Approved, CreatedAt = DateTime.UtcNow
            });

        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs/1/approve", "ROLE_JOB_APPROVE",
            JsonContent.Create(new { }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JobApprove_can_reject_job_definition()
    {
        factory.JobDefinitions.RejectAsync(1, Arg.Any<RejectJobDefinitionDto>(), Arg.Any<CancellationToken>())
            .Returns(new JobDefinitionDetailDto
            {
                Id = 1, Name = "Job", ScriptPath = "job.sh", CronExpression = "* * * * *", RunAsUser = "svc",
                CreatedBy = "creator", ApprovalStatus = JobApprovalStatus.Rejected, CreatedAt = DateTime.UtcNow
            });

        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs/1/reject", "ROLE_JOB_APPROVE",
            JsonContent.Create(new { reason = "needs rework" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JobApprove_cannot_create_job_definition()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/jobs", "ROLE_JOB_APPROVE",
            JsonContent.Create(new { }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task JobApprove_cannot_edit_job_definition()
    {
        using var request = await WithRolesAndCsrfAsync(HttpMethod.Put, "/api/jobs/1", "ROLE_JOB_APPROVE",
            JsonContent.Create(new { }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- ROLE_ADMIN implies every permission ----

    [Fact]
    public async Task Admin_can_search_execution_logs()
    {
        factory.Execution.SearchAsync(Arg.Any<ExecutionLogFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ExecutionLogListItemDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 });

        var response = await _client.SendAsync(WithRoles(HttpMethod.Get, "/api/execution-logs", "ROLE_ADMIN"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_can_create_category_without_holding_category_write_explicitly()
    {
        factory.Categories.CreateAsync(Arg.Any<CreateCategoryDto>(), Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto { Id = 99, Description = "Test", CreatedAt = DateTime.UtcNow });

        using var request = await WithRolesAndCsrfAsync(HttpMethod.Post, "/api/categories", "ROLE_ADMIN",
            JsonContent.Create(new { description = "Test" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
