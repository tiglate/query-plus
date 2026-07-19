using FluentAssertions;
using QueryPlus.Application.DTOs.Common;

namespace QueryPlus.Application.Tests;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-1, 20, 1, 20)]
    [InlineData(3, 0, 3, 20)]
    [InlineData(1, 200, 1, 100)]
    [InlineData(2, 10, 2, 10)]
    public void Normalize_ClampsPageAndPageSize(int page, int pageSize, int expectedPage, int expectedSize)
    {
        var (p, s) = PagedResult<object>.Normalize(page, pageSize);
        p.Should().Be(expectedPage);
        s.Should().Be(expectedSize);
    }

    [Fact]
    public void Normalize_WithTotalCount_ClampsPastEndPage()
    {
        var (page, pageSize) = PagedResult<object>.Normalize(page: 10, pageSize: 20, totalCount: 25);
        page.Should().Be(2);
        pageSize.Should().Be(20);
    }

    [Fact]
    public void TotalPages_AndNavigationFlags()
    {
        var result = new PagedResult<int>
        {
            Items = [1, 2],
            TotalCount = 25,
            Page = 2,
            PageSize = 10
        };

        result.TotalPages.Should().Be(3);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeTrue();
    }
}
