using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using QueryPlus.Data.Metadata;

namespace QueryPlus.Data.Tests;

public class SqlProcedureMetadataSyncServiceTests
{
    private readonly IConfiguration _config = Substitute.For<IConfiguration>();

    [Fact]
    public void Constructor_MissingConnectionString_ThrowsInvalidOperationException()
    {
        _config.GetSection("ConnectionStrings")["DefaultConnection"].Returns((string?)null);

        Action act = () => _ = new SqlProcedureMetadataSyncService(_config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*DefaultConnection*");
    }

    [Fact]
    public async Task FetchAsync_InvalidDatabaseName_ThrowsArgumentException()
    {
        _config.GetSection("ConnectionStrings")["DefaultConnection"].Returns("Server=localhost;Database=master;");
        var sut = new SqlProcedureMetadataSyncService(_config);

        Func<Task> act = async () => await sut.FetchAsync("DB; DROP TABLE--", "sp_sales");

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Invalid database name*");
    }

    [Fact]
    public async Task FetchAsync_InvalidProcedureName_ThrowsArgumentException()
    {
        _config.GetSection("ConnectionStrings")["DefaultConnection"].Returns("Server=localhost;Database=master;");
        var sut = new SqlProcedureMetadataSyncService(_config);

        Func<Task> act = async () => await sut.FetchAsync("SalesDB", "dbo.sp_sales; DROP TABLE--");

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Invalid procedure name*");
    }
}
