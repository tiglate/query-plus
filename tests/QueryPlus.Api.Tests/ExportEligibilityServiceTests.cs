using System.Collections;
using System.Reflection;
using FluentAssertions;
using QueryPlus.Api.Services;

namespace QueryPlus.Api.Tests;

public class ExportEligibilityServiceTests
{
    private readonly ExportEligibilityService _sut = new();

    /// <summary>
    /// ExportEligibilityService has no injectable clock (Ttl is a hardcoded 30-minute constant),
    /// so TTL-expiry and the otherwise-unreachable-via-the-public-API RowCount&lt;=0 branch are
    /// exercised by reflecting into the private entries dictionary rather than sleeping for real
    /// or adding a clock seam to production code. This mutates only the instance created by the
    /// test itself, so it is not vulnerable to cross-test/parallelism interference.
    /// </summary>
    private static void MutateStoredEntry(ExportEligibilityService sut, string username, DateTime? createdAt = null, int? rowCount = null)
    {
        var entriesField = typeof(ExportEligibilityService).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var entries = (IDictionary)entriesField.GetValue(sut)!;
        var key = username.Trim().ToLowerInvariant();
        var current = entries[key]!;
        var entryType = current.GetType();
        var procedureId = (int)entryType.GetProperty("ProcedureId")!.GetValue(current)!;
        var parameterHash = (string)entryType.GetProperty("ParameterHash")!.GetValue(current)!;
        var currentRowCount = (int)entryType.GetProperty("RowCount")!.GetValue(current)!;
        var currentCreatedAt = (DateTime)entryType.GetProperty("CreatedAt")!.GetValue(current)!;

        var ctor = entryType.GetConstructors()[0];
        entries[key] = ctor.Invoke([
            procedureId,
            parameterHash,
            rowCount ?? currentRowCount,
            createdAt ?? currentCreatedAt
        ]);
    }

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

    [Theory]
    [InlineData("", 5, 10)] // blank username
    [InlineData("user5", 0, 10)] // procedureId <= 0
    [InlineData("user5", 5, 0)] // rowCount <= 0
    public void MarkEligible_WithInvalidInputs_DoesNotStoreAnEntry(string username, int procedureId, int rowCount)
    {
        var paramsDict = new Dictionary<string, string?> { ["@Val"] = "abc" };

        _sut.MarkEligible(username, procedureId, paramsDict, rowCount);

        var isValid = _sut.TryValidate(string.IsNullOrEmpty(username) ? "user5" : username, procedureId <= 0 ? 5 : procedureId, paramsDict, out var error);

        isValid.Should().BeFalse();
        error.Should().Be("export-not-eligible");
    }

    [Fact]
    public void TryValidate_ReturnsError_WhenEntryHasExpired()
    {
        var username = "user6";
        var paramsDict = new Dictionary<string, string?> { ["@Val"] = "abc" };
        _sut.MarkEligible(username, procedureId: 5, paramsDict, rowCount: 10);

        MutateStoredEntry(_sut, username, createdAt: DateTime.UtcNow.AddMinutes(-31));

        var isValid = _sut.TryValidate(username, procedureId: 5, paramsDict, out var error);

        isValid.Should().BeFalse();
        error.Should().Be("export-expired");

        // Expiry also evicts the entry, so a follow-up call reports not-eligible rather than expired again.
        var secondCall = _sut.TryValidate(username, procedureId: 5, paramsDict, out var secondError);
        secondCall.Should().BeFalse();
        secondError.Should().Be("export-not-eligible");
    }

    [Fact]
    public void TryValidate_ReturnsError_WhenStoredRowCountIsNotPositive()
    {
        // MarkEligible itself never stores a RowCount<=0 entry (it routes to Clear instead), so
        // this branch is only reachable by mutating stored state directly.
        var username = "user7";
        var paramsDict = new Dictionary<string, string?> { ["@Val"] = "abc" };
        _sut.MarkEligible(username, procedureId: 5, paramsDict, rowCount: 10);

        MutateStoredEntry(_sut, username, rowCount: 0);

        var isValid = _sut.TryValidate(username, procedureId: 5, paramsDict, out var error);

        isValid.Should().BeFalse();
        error.Should().Be("export-no-rows");
    }

    [Fact]
    public void MarkEligible_And_TryValidate_HashIsOrderIndependent()
    {
        var username = "user8";
        var inOneOrder = new Dictionary<string, string?> { ["@Category"] = "Electronics", ["@Active"] = "1" };
        var inAnotherOrder = new Dictionary<string, string?> { ["@Active"] = "1", ["@Category"] = "Electronics" };

        _sut.MarkEligible(username, procedureId: 5, inOneOrder, rowCount: 10);

        var isValid = _sut.TryValidate(username, procedureId: 5, inAnotherOrder, out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }
}
