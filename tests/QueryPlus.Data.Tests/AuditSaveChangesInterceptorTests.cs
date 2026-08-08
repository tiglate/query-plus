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
}
