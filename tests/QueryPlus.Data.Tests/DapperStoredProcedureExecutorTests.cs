using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using QueryPlus.Data.StoredProcedures;

namespace QueryPlus.Data.Tests;

public class DapperStoredProcedureExecutorTests
{
    private readonly IConfiguration _config = Substitute.For<IConfiguration>();

    [Fact]
    public void Constructor_MissingConnectionString_ThrowsInvalidOperationException()
    {
        _config.GetSection("ConnectionStrings")["DefaultConnection"].Returns((string?)null);

        Action act = () => _ = new DapperStoredProcedureExecutor(_config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*DefaultConnection*");
    }

    [Fact]
    public async Task ExecuteAsync_InvalidDatabaseName_ThrowsArgumentException()
    {
        _config.GetSection("ConnectionStrings")["DefaultConnection"].Returns("Server=localhost;Database=master;");
        var sut = new DapperStoredProcedureExecutor(_config);

        Func<Task> act = async () => await sut.ExecuteAsync("DB; DROP TABLE--", "sp_test", new Dictionary<string, object?>());

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
