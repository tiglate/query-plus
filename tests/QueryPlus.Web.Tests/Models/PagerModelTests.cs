using FluentAssertions;
using QueryPlus.Web.Models;

namespace QueryPlus.Web.Tests.Models;

public sealed class PagerModelTests
{
    [Fact]
    public void FromItem_and_ToItem_cover_page_window()
    {
        var pager = new PagerModel
        {
            Page = 2,
            PageSize = 20,
            TotalCount = 45,
            TotalPages = 3,
            Controller = "Categories"
        };

        pager.FromItem.Should().Be(21);
        pager.ToItem.Should().Be(40);
        pager.HasPrevious.Should().BeTrue();
        pager.HasNext.Should().BeTrue();
    }

    [Fact]
    public void VisiblePages_clamps_to_window()
    {
        var pager = new PagerModel
        {
            Page = 5,
            PageSize = 10,
            TotalCount = 100,
            TotalPages = 10,
            Controller = "Procedures"
        };

        pager.VisiblePages(window: 1).Should().Equal(4, 5, 6);
    }

    [Fact]
    public void Empty_total_has_zero_range()
    {
        var pager = new PagerModel
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 0,
            Controller = "Categories"
        };

        pager.FromItem.Should().Be(0);
        pager.ToItem.Should().Be(0);
        pager.HasPrevious.Should().BeFalse();
        pager.HasNext.Should().BeFalse();
    }
}
