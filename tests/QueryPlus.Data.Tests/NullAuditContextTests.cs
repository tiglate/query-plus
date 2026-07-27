using FluentAssertions;
using QueryPlus.Data.Interceptors;

namespace QueryPlus.Data.Tests;

public class NullAuditContextTests
{
    [Fact]
    public void NullAuditContext_ReturnsDefaultValues()
    {
        var sut = new NullAuditContext();

        sut.Username.Should().Be("system");
        sut.IpAddress.Should().BeNull();
    }
}
