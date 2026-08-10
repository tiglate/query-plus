using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QueryPlus.Application.Abstractions;
using QueryPlus.Data.Context;
using QueryPlus.Data.Interceptors;

using Microsoft.EntityFrameworkCore.Diagnostics;
using QueryPlus.Domain.Common;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Tests;

public class AuditSaveChangesInterceptorTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();

    public AuditSaveChangesInterceptorTests()
    {
        _currentUser.Username.Returns("audit_user");
        _currentUser.IpAddress.Returns("127.0.0.1");

        var interceptor = new AuditSaveChangesInterceptor(_currentUser);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task SaveChangesAsync_CreatesRevision_And_AuditRows_ForCategoryAndProcedure()
    {
        var category = new Category { Description = "Audit Category" };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var rev = await _db.Set<Revision>().FirstOrDefaultAsync();
        rev.Should().NotBeNull();
        rev!.Username.Should().Be("audit_user");

        var catAud = await _db.Set<CategoryAud>().FirstOrDefaultAsync();
        catAud.Should().NotBeNull();
        catAud!.Description.Should().Be("Audit Category");
        catAud.IdRevisionType.Should().Be(RevisionTypeCode.Insert);

        // Update
        category.Description = "Updated Audit Category";
        await _db.SaveChangesAsync();

        var updatedAud = await _db.Set<CategoryAud>().Where(a => a.IdRevisionType == RevisionTypeCode.Update).FirstOrDefaultAsync();
        updatedAud.Should().NotBeNull();
        updatedAud!.Description.Should().Be("Updated Audit Category");
    }

    [Fact]
    public async Task SaveChangesAsync_CreatesAuditRows_ForProcedureParametersAndColumns()
    {
        var category = new Category { Description = "Cat" };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var param = new ProcedureParameter { Caption = "P1", Name = "@P1", ParameterType = ParameterType.FreeText, IsRequired = true, IsSensitive = true };
        var col = new ProcedureColumn { TechnicalName = "C1", Caption = "C1", Alignment = ColumnAlignment.Left, Visible = true };

        var proc = new Procedure
        {
            IdCategory = category.IdCategory,
            Caption = "Audit Proc",
            ConnectionName = "DefaultConnection",
            DatabaseName = "DB",
            ProcedureName = "sp_audit",
            RoleEntitlement = "user",
            Parameters = [param],
            Columns = [col]
        };

        _db.Procedures.Add(proc);
        await _db.SaveChangesAsync();

        var procAud = await _db.Set<ProcedureAud>().FirstOrDefaultAsync(a => a.IdRevisionType == RevisionTypeCode.Insert);
        procAud.Should().NotBeNull();
        procAud!.Caption.Should().Be("Audit Proc");
        procAud.ConnectionName.Should().Be("DefaultConnection");

        var paramAud = await _db.Set<ProcedureParameterAud>().FirstOrDefaultAsync(a => a.IdRevisionType == RevisionTypeCode.Insert);
        paramAud.Should().NotBeNull();
        paramAud!.Caption.Should().Be("P1");
        // is_sensitive is itself a governance/security classification flag - if the
        // interceptor stops populating it, changes to which parameters are marked sensitive
        // would silently drop out of the audit trail.
        paramAud.IsSensitive.Should().BeTrue();

        var colAud = await _db.Set<ProcedureColumnAud>().FirstOrDefaultAsync(a => a.IdRevisionType == RevisionTypeCode.Insert);
        colAud.Should().NotBeNull();
        colAud!.Caption.Should().Be("C1");

        // Delete parameter and column
        _db.ProcedureParameters.Remove(param);
        _db.ProcedureColumns.Remove(col);
        await _db.SaveChangesAsync();

        var delParamAud = await _db.Set<ProcedureParameterAud>().FirstOrDefaultAsync(a => a.IdRevisionType == RevisionTypeCode.Delete);
        delParamAud.Should().NotBeNull();

        var delColAud = await _db.Set<ProcedureColumnAud>().FirstOrDefaultAsync(a => a.IdRevisionType == RevisionTypeCode.Delete);
        delColAud.Should().NotBeNull();
    }

    private static JobDefinition BuildJobDefinition() => new()
    {
        Name = "nightly-cleanup",
        Description = "Removes stale temp files",
        JobType = JobType.Shell,
        ScriptPath = "/opt/queryplus/jobs/nightly-cleanup.sh",
        CronExpression = "0 2 * * *",
        RunAsUser = "queryplus-jobs",
        MemoryLimitMb = 512,
        MaxDurationMinutes = 30,
        Enabled = true,
        ApprovalStatus = JobApprovalStatus.Draft,
        CreatedBy = "analyst1",
        NotifyEmails = "analyst1@example.com"
    };

    [Fact]
    public async Task SaveChangesAsync_CreatesRevisionAndAuditRow_ForJobDefinitionInsert_AndCorrectsTemporaryKey()
    {
        var job = BuildJobDefinition();
        _db.JobDefinitions.Add(job);
        await _db.SaveChangesAsync();

        job.IdJobDefinition.Should().NotBe(0);

        var jobAud = await _db.Set<JobDefinitionAud>()
            .FirstOrDefaultAsync(a => a.IdRevisionType == RevisionTypeCode.Insert);

        jobAud.Should().NotBeNull();
        jobAud!.IdRevisionType.Should().Be(RevisionTypeCode.Insert);

        // Exercises the async temporary-key correction path: the aud row must resolve to the
        // real generated id, not the negative placeholder EF assigns before SaveChangesAsync.
        jobAud.IdJobDefinition.Should().Be(job.IdJobDefinition);

        jobAud.Name.Should().Be("nightly-cleanup");
        jobAud.Description.Should().Be("Removes stale temp files");
        jobAud.JobType.Should().Be(JobType.Shell.ToString());
        jobAud.ScriptPath.Should().Be("/opt/queryplus/jobs/nightly-cleanup.sh");
        jobAud.ScriptSha256.Should().BeNull();
        jobAud.CronExpression.Should().Be("0 2 * * *");
        jobAud.RunAsUser.Should().Be("queryplus-jobs");
        jobAud.MemoryLimitMb.Should().Be(512);
        jobAud.MaxDurationMinutes.Should().Be(30);
        jobAud.Enabled.Should().BeTrue();
        jobAud.ApprovalStatus.Should().Be(JobApprovalStatus.Draft.ToString());
        jobAud.CreatedBy.Should().Be("analyst1");
        jobAud.ApprovedBy.Should().BeNull();
        jobAud.ApprovedAt.Should().BeNull();
        jobAud.RejectionReason.Should().BeNull();
        jobAud.NotifyEmails.Should().Be("analyst1@example.com");
        jobAud.CreatedAt.Should().NotBeNull();

        var rev = await _db.Set<Revision>().FirstOrDefaultAsync(r => r.IdRevision == jobAud.IdRevision);
        rev.Should().NotBeNull();
        rev!.Username.Should().Be("audit_user");

        var jobAudCount = await _db.Set<JobDefinitionAud>().CountAsync();
        jobAudCount.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_CreatesAuditRow_ForJobDefinitionUpdate()
    {
        var job = BuildJobDefinition();
        _db.JobDefinitions.Add(job);
        await _db.SaveChangesAsync();

        job.Description = "Removes stale temp files older than 7 days";
        job.ApprovalStatus = JobApprovalStatus.PendingApproval;
        await _db.SaveChangesAsync();

        var updatedAud = await _db.Set<JobDefinitionAud>()
            .Where(a => a.IdRevisionType == RevisionTypeCode.Update)
            .FirstOrDefaultAsync();

        updatedAud.Should().NotBeNull();
        updatedAud!.IdJobDefinition.Should().Be(job.IdJobDefinition);
        updatedAud.Description.Should().Be("Removes stale temp files older than 7 days");
        updatedAud.ApprovalStatus.Should().Be(JobApprovalStatus.PendingApproval.ToString());
        // Unchanged columns must still round-trip on an update revision.
        updatedAud.Name.Should().Be("nightly-cleanup");
        updatedAud.JobType.Should().Be(JobType.Shell.ToString());

        var jobAudCount = await _db.Set<JobDefinitionAud>().CountAsync();
        jobAudCount.Should().Be(2);
    }

    [Fact]
    public async Task SaveChangesAsync_CreatesAuditRow_ForJobDefinitionDelete_UsingOriginalValues()
    {
        var job = BuildJobDefinition();
        _db.JobDefinitions.Add(job);
        await _db.SaveChangesAsync();
        var realId = job.IdJobDefinition;

        _db.JobDefinitions.Remove(job);
        await _db.SaveChangesAsync();

        var deletedAud = await _db.Set<JobDefinitionAud>()
            .FirstOrDefaultAsync(a => a.IdRevisionType == RevisionTypeCode.Delete);

        deletedAud.Should().NotBeNull();
        deletedAud!.IdJobDefinition.Should().Be(realId);
        deletedAud.Name.Should().Be("nightly-cleanup");
        deletedAud.JobType.Should().Be(JobType.Shell.ToString());
        deletedAud.ApprovalStatus.Should().Be(JobApprovalStatus.Draft.ToString());
        deletedAud.CreatedBy.Should().Be("analyst1");

        var jobAudCount = await _db.Set<JobDefinitionAud>().CountAsync();
        jobAudCount.Should().Be(2);
    }
}
