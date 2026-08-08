using FluentAssertions;
using QueryPlus.Api.Hosting;

namespace QueryPlus.Api.Tests;

public sealed class EnvFileLoaderTests : IDisposable
{
    private readonly string _root;
    private readonly List<string> _envVarsToClear = [];

    public EnvFileLoaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "QueryPlusEnvTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        foreach (var key in _envVarsToClear)
        {
            Environment.SetEnvironmentVariable(key, null);
        }

        if (Directory.Exists(_root))
        {
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
    }

    private string Track(string key)
    {
        _envVarsToClear.Add(key);
        return key;
    }

    [Fact]
    public void LoadFromAncestors_FindsEnvInAnAncestorDirectory()
    {
        var key = Track("QP_TEST_ANCESTOR_" + Guid.NewGuid().ToString("N"));
        var deep = Directory.CreateDirectory(Path.Combine(_root, "a", "b", "c")).FullName;
        File.WriteAllText(Path.Combine(_root, ".env"), $"{key}=found");

        EnvFileLoader.LoadFromAncestors(deep);

        Environment.GetEnvironmentVariable(key).Should().Be("found");
    }

    [Fact]
    public void LoadFromAncestors_StopsAtTheRepoRootMarker_AndDoesNotSeeAnEnvAboveIt()
    {
        var key = Track("QP_TEST_ABOVE_ROOT_" + Guid.NewGuid().ToString("N"));
        // .env sits ABOVE the repo root (marked by QueryPlus.sln) - must never be picked up.
        File.WriteAllText(Path.Combine(_root, ".env"), $"{key}=leaked");
        var repoRoot = Directory.CreateDirectory(Path.Combine(_root, "repo")).FullName;
        File.WriteAllText(Path.Combine(repoRoot, "QueryPlus.sln"), "");
        var deep = Directory.CreateDirectory(Path.Combine(repoRoot, "src", "QueryPlus.Api")).FullName;

        EnvFileLoader.LoadFromAncestors(deep);

        Environment.GetEnvironmentVariable(key).Should().BeNull();
    }

    [Fact]
    public void LoadFromAncestors_LoadsEnvInsideTheRepoRoot_EvenWithMarkerPresent()
    {
        var key = Track("QP_TEST_AT_ROOT_" + Guid.NewGuid().ToString("N"));
        var repoRoot = Directory.CreateDirectory(Path.Combine(_root, "repo2")).FullName;
        File.WriteAllText(Path.Combine(repoRoot, "QueryPlus.sln"), "");
        File.WriteAllText(Path.Combine(repoRoot, ".env"), $"{key}=root-value");
        var deep = Directory.CreateDirectory(Path.Combine(repoRoot, "src", "QueryPlus.Api")).FullName;

        EnvFileLoader.LoadFromAncestors(deep);

        Environment.GetEnvironmentVariable(key).Should().Be("root-value");
    }

    [Fact]
    public void LoadFromAncestors_NeverWalksPastTheAbsoluteLevelCap_WhenNoMarkerExists()
    {
        var key = Track("QP_TEST_TOO_DEEP_" + Guid.NewGuid().ToString("N"));
        // No QueryPlus.sln anywhere - simulates a published container image's /app tree.
        // 10 levels deep exceeds EnvFileLoader's absolute MaxLevels safety net (8).
        var deep = _root;
        for (var i = 0; i < 10; i++)
        {
            deep = Directory.CreateDirectory(Path.Combine(deep, $"lvl{i}")).FullName;
        }

        File.WriteAllText(Path.Combine(_root, ".env"), $"{key}=too-far");

        EnvFileLoader.LoadFromAncestors(deep);

        Environment.GetEnvironmentVariable(key).Should().BeNull();
    }
}
