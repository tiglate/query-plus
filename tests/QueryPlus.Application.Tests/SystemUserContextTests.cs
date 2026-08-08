using FluentAssertions;
using QueryPlus.Application.Abstractions;

namespace QueryPlus.Application.Tests;

public class SystemUserContextTests
{
    private readonly SystemUserContext _sut = new();

    [Fact]
    public void IsAuthenticated_IsAlwaysTrue() => _sut.IsAuthenticated.Should().BeTrue();

    [Fact]
    public void Username_IsSystem() => _sut.Username.Should().Be("system");

    [Fact]
    public void IpAddress_IsNull() => _sut.IpAddress.Should().BeNull();

    [Fact]
    public void Roles_ContainsOnlyAdminRole() => _sut.Roles.Should().ContainSingle().Which.Should().Be("ROLE_ADMIN");

    [Theory]
    [InlineData("ROLE_ADMIN", true)]
    [InlineData("role_admin", true)]
    [InlineData("ROLE_QUERY_EXEC", false)]
    public void IsInRole_IsCaseInsensitive_AndOnlyMatchesAdmin(string role, bool expected) =>
        _sut.IsInRole(role).Should().Be(expected);
}
