using FluentAssertions;
using NSubstitute;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Services;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Tests;

public class ProcedureServiceTests
{
    private readonly IProcedureRepository _procedures = Substitute.For<IProcedureRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IConfigurationAuditReader _auditReader = Substitute.For<IConfigurationAuditReader>();
    private readonly ProcedureService _sut;

    public ProcedureServiceTests()
    {
        _auditReader.GetProcedureAuditDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new AuditDetailsDto { CreatedBy = "admin", UpdatedBy = "admin" });

        _sut = new ProcedureService(
            _procedures,
            _categories,
            _unitOfWork,
            _currentUser,
            _auditReader,
            new SaveProcedureDtoValidator());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProcedureDetail_WhenFound()
    {
        var proc = new Procedure
        {
            IdProcedure = 1,
            IdCategory = 1,
            Caption = "Monthly Sales",
            DatabaseName = "DB",
            ProcedureName = "sp_sales",
            RoleEntitlement = ""
        };
        _procedures.GetByIdWithDetailsAsync(1, Arg.Any<CancellationToken>()).Returns(proc);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Caption.Should().Be("Monthly Sales");
    }

    [Fact]
    public async Task CreateAsync_CategoryNotFound_ThrowsEntityNotFoundException()
    {
        _categories.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var dto = new SaveProcedureDto
        {
            CategoryId = 99,
            Caption = "Test Caption",
            DatabaseName = "DB",
            ProcedureName = "sp_test",
            RoleEntitlement = "user"
        };

        Func<Task> act = async () => await _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateCaption_ThrowsValidationException()
    {
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Category { IdCategory = 1, Description = "Gen" });
        _procedures.ExistsByCaptionAsync("Test Caption", null, Arg.Any<CancellationToken>()).Returns(true);

        var dto = new SaveProcedureDto
        {
            CategoryId = 1,
            Caption = "Test Caption",
            DatabaseName = "DB",
            ProcedureName = "sp_test",
            RoleEntitlement = "user"
        };

        Func<Task> act = async () => await _sut.CreateAsync(dto);

        var exc = await act.Should().ThrowAsync<Common.ValidationException>();
        exc.Which.Errors.Should().ContainKey("Caption");
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsEntityNotFoundException()
    {
        _procedures.GetByIdWithDetailsAsync(999, Arg.Any<CancellationToken>()).Returns((Procedure?)null);

        Func<Task> act = async () => await _sut.DeleteAsync(999);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ExistingProcedure_RemovesEntity()
    {
        var proc = new Procedure { IdProcedure = 5, IdCategory = 1, Caption = "Del", DatabaseName = "DB", ProcedureName = "sp_del", RoleEntitlement = "" };
        _procedures.GetByIdWithDetailsAsync(5, Arg.Any<CancellationToken>()).Returns(proc);

        await _sut.DeleteAsync(5);

        _procedures.Received(1).Remove(proc);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NullOrZeroId_ThrowsValidationException()
    {
        var dto = new SaveProcedureDto { Id = null, CategoryId = 1, Caption = "C", DatabaseName = "DB", ProcedureName = "sp_c", RoleEntitlement = "" };

        Func<Task> act = async () => await _sut.UpdateAsync(dto);

        await act.Should().ThrowAsync<Common.ValidationException>();
    }

    [Fact]
    public async Task GetAccessibleForCurrentUserAsync_ReturnsAccessibleProcedures()
    {
        _currentUser.Roles.Returns(["user"]);
        var list = new List<Procedure>
        {
            new() { IdProcedure = 1, IdCategory = 1, Caption = "Accessible", DatabaseName = "DB", ProcedureName = "sp_a", RoleEntitlement = "user" }
        };
        _procedures.GetAccessibleForExecutionAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(list);

        var result = await _sut.GetAccessibleForCurrentUserAsync();

        result.Should().ContainSingle(p => p.Caption == "Accessible");
    }

    [Fact]
    public async Task SearchAsync_ReturnsPagedProcedures()
    {
        var list = new List<Procedure>
        {
            new() { IdProcedure = 1, IdCategory = 1, Caption = "Found", DatabaseName = "DB", ProcedureName = "sp_f", RoleEntitlement = "" }
        };
        _procedures.SearchAsync(Arg.Any<ProcedureSearchCriteria>(), 1, 10, Arg.Any<CancellationToken>())
            .Returns((list, 1));

        var result = await _sut.SearchAsync(new ProcedureFilterDto { Caption = "Found", Page = 1, PageSize = 10 });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Caption == "Found");
    }
}
