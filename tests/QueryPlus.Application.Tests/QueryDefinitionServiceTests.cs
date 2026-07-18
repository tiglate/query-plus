using FluentAssertions;
using NSubstitute;
using QueryPlus.Application.Services;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Tests;

public class QueryDefinitionServiceTests
{
    private readonly IRepository<QueryDefinition> _repository = Substitute.For<IRepository<QueryDefinition>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly QueryDefinitionService _sut;

    public QueryDefinitionServiceTests()
    {
        _sut = new QueryDefinitionService(_repository, _unitOfWork);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenFound()
    {
        var entity = new QueryDefinition
        {
            Id = 1,
            Name = "Sample",
            StoredProcedureName = "dbo.usp_Sample"
        };
        _repository.GetByIdAsync(1).Returns(entity);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Sample");
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAt_AndSaves()
    {
        var definition = new QueryDefinition
        {
            Name = "New Query",
            StoredProcedureName = "dbo.usp_New"
        };
        _repository.AddAsync(definition).Returns(definition);

        var result = await _sut.CreateAsync(definition);

        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenNotFound()
    {
        _repository.GetByIdAsync(99).Returns((QueryDefinition?)null);

        var act = async () => await _sut.DeleteAsync(99);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
