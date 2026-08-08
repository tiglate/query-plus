using FluentAssertions;
using QueryPlus.Api.Services;

namespace QueryPlus.Api.Tests;

public class ExportEligibilityServiceTests
{
    private readonly ExportEligibilityService _sut = new();

    [Fact]
    public void MarkEligible_And_TryValidate_ReturnsTrue_WhenParametersMatch()
    {
        var username = "user1";
        var procId = 10;
        var paramsDict = new Dictionary<string, string?> { ["@Category"] = "Electronics", ["@Active"] = "1" };

        _sut.MarkEligible(username, procId, paramsDict, rowCount: 150);

        var isValid = _sut.TryValidate(username, procId, paramsDict, out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void TryValidate_ReturnsError_WhenParametersMismatch()
    {
        var username = "user2";
        var procId = 10;
        var originalParams = new Dictionary<string, string?> { ["@Date"] = "2026-01-01" };
        var differentParams = new Dictionary<string, string?> { ["@Date"] = "2026-02-01" };

        _sut.MarkEligible(username, procId, originalParams, rowCount: 50);

        var isValid = _sut.TryValidate(username, procId, differentParams, out var error);

        isValid.Should().BeFalse();
        error.Should().Be("export-params-mismatch");
    }

    [Fact]
    public void TryValidate_ReturnsError_WhenProcedureIdMismatch()
    {
        var username = "user3";
        var paramsDict = new Dictionary<string, string?> { ["@Region"] = "US" };

        _sut.MarkEligible(username, procedureId: 10, paramsDict, rowCount: 20);

        var isValid = _sut.TryValidate(username, procedureId: 99, paramsDict, out var error);

        isValid.Should().BeFalse();
        error.Should().Be("export-procedure-mismatch");
    }

    [Fact]
    public void Clear_RemovesEligibility()
    {
        var username = "user4";
        var paramsDict = new Dictionary<string, string?> { ["@Val"] = "abc" };

        _sut.MarkEligible(username, procedureId: 5, paramsDict, rowCount: 10);
        _sut.Clear(username);

        var isValid = _sut.TryValidate(username, procedureId: 5, paramsDict, out var error);

        isValid.Should().BeFalse();
        error.Should().Be("export-not-eligible");
    }
}
