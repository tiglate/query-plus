using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using QueryPlus.Data.Metadata;

namespace QueryPlus.Data.Tests;

public class SqlProcedureMetadataSyncServiceTests
{
    private readonly IConfiguration _config = Substitute.For<IConfiguration>();

    [Fact]
    public async Task FetchAsync_MissingConnectionString_ThrowsInvalidOperationException()
    {
        _config.GetSection("ConnectionStrings")["Server2"].Returns((string?)null);
        var sut = new SqlProcedureMetadataSyncService(_config);

        Func<Task> act = async () => await sut.FetchAsync("Server2", "SalesDB", "sp_sales");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Server2*");
    }

    [Fact]
    public async Task FetchAsync_InvalidDatabaseName_ThrowsArgumentException()
    {
        _config.GetSection("ConnectionStrings")["DefaultConnection"].Returns("Server=localhost;Database=master;");
        var sut = new SqlProcedureMetadataSyncService(_config);

        Func<Task> act = async () => await sut.FetchAsync("DefaultConnection", "DB; DROP TABLE--", "sp_sales");

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Invalid database name*");
    }

    [Fact]
    public async Task FetchAsync_InvalidProcedureName_ThrowsArgumentException()
    {
        _config.GetSection("ConnectionStrings")["DefaultConnection"].Returns("Server=localhost;Database=master;");
        var sut = new SqlProcedureMetadataSyncService(_config);

        Func<Task> act = async () => await sut.FetchAsync("DefaultConnection", "SalesDB", "dbo.sp_sales; DROP TABLE--");

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Invalid procedure name*");
    }
}
