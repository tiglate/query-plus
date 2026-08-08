using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using QueryPlus.Data.StoredProcedures;

namespace QueryPlus.Data.Tests;

public class DapperStoredProcedureExecutorTests
{
    private readonly IConfiguration _config = Substitute.For<IConfiguration>();

    [Fact]
    public async Task ExecuteAsync_MissingConnectionString_ThrowsInvalidOperationException()
    {
        _config.GetSection("ConnectionStrings")["Server2"].Returns((string?)null);
        var sut = new DapperStoredProcedureExecutor(_config);

        Func<Task> act = async () =>
            await sut.ExecuteAsync("Server2", "db", "sp_test", new Dictionary<string, object?>());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Server2*");
    }

    [Fact]
    public async Task ExecuteAsync_InvalidDatabaseName_ThrowsArgumentException()
    {
        _config.GetSection("ConnectionStrings")["DefaultConnection"].Returns("Server=localhost;Database=master;");
        var sut = new DapperStoredProcedureExecutor(_config);

        Func<Task> act = async () =>
            await sut.ExecuteAsync("DefaultConnection", "DB; DROP TABLE--", "sp_test", new Dictionary<string, object?>());

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
