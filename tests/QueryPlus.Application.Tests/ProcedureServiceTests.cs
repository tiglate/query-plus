using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Mapping;
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
    private readonly ICurrentUserContext _user = Substitute.For<ICurrentUserContext>();
    private readonly ProcedureService _sut;

    public ProcedureServiceTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<QueryPlusMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        _user.Roles.Returns(["user"]);
        _sut = new ProcedureService(
            _procedures,
            _categories,
            _unitOfWork,
            mapper,
            _user,
            new SaveProcedureDtoValidator());
    }

    [Fact]
    public async Task SearchAsync_ReturnsPagedResult()
    {
        var entities = new List<Procedure>
        {
            new()
            {
                IdProcedure = 1,
                IdCategory = 1,
                Caption = "Sales Report",
                DatabaseName = "Sales",
                ProcedureName = "dbo.usp_Sales",
                RoleEntitlement = "user",
                Category = new Category { IdCategory = 1, Description = "Sales" }
            }
        };
        _procedures.SearchAsync(Arg.Any<ProcedureSearchCriteria>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((entities, 1));

        var result = await _sut.SearchAsync(new ProcedureFilterDto
        {
            Caption = "Sales",
            Page = 1,
            PageSize = 20
        });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Caption == "Sales Report");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        _procedures.GetByIdWithDetailsAsync(1).Returns(new Procedure
        {
            IdProcedure = 1,
            IdCategory = 1,
            Caption = "Sample",
            DatabaseName = "Sales",
            ProcedureName = "dbo.usp_Sample",
            RoleEntitlement = "user",
            Category = new Category { IdCategory = 1, Description = "Sales" }
        });

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Caption.Should().Be("Sample");
        result.CategoryDescription.Should().Be("Sales");
    }

    [Fact]
    public async Task CreateAsync_Saves_WhenValid()
    {
        _categories.GetByIdAsync(1).Returns(new Category { IdCategory = 1, Description = "Cat" });
        _procedures.ExistsByCaptionAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _procedures.ExistsByDatabaseAndNameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _procedures.When(x => x.AddAsync(Arg.Any<Procedure>(), Arg.Any<CancellationToken>()))
            .Do(ci =>
            {
                var p = ci.ArgAt<Procedure>(0);
                p.IdProcedure = 10;
            });
        _procedures.GetByIdWithDetailsAsync(10).Returns(ci => new Procedure
        {
            IdProcedure = 10,
            IdCategory = 1,
            Caption = "New Procedure",
            DatabaseName = "Sales",
            ProcedureName = "usp_New",
            RoleEntitlement = "user",
            Category = new Category { IdCategory = 1, Description = "Cat" }
        });

        var dto = new SaveProcedureDto
        {
            CategoryId = 1,
            Caption = "New Procedure",
            DatabaseName = "Sales",
            ProcedureName = "usp_New",
            RoleEntitlement = "user"
        };

        var result = await _sut.CreateAsync(dto);

        result.Id.Should().Be(10);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenNotFound()
    {
        _procedures.GetByIdWithDetailsAsync(99).Returns((Procedure?)null);

        var act = async () => await _sut.DeleteAsync(99);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task ListAllAsync_MapsAllProcedures()
    {
        _procedures.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new Procedure
            {
                IdProcedure = 1,
                IdCategory = 1,
                Caption = "Alpha",
                DatabaseName = "Sales",
                ProcedureName = "dbo.usp_Alpha",
                RoleEntitlement = "user"
            },
            new Procedure
            {
                IdProcedure = 2,
                IdCategory = 1,
                Caption = "Beta",
                DatabaseName = "Sales",
                ProcedureName = "dbo.usp_Beta",
                RoleEntitlement = "user"
            }
        ]);

        var result = await _sut.ListAllAsync();

        result.Should().HaveCount(2);
        result.Select(p => p.Caption).Should().Equal("Alpha", "Beta");
    }
}
