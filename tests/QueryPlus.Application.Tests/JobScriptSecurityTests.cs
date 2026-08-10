using FluentAssertions;
using QueryPlus.Application.Common;

namespace QueryPlus.Application.Tests;

public sealed class JobScriptSecurityTests : IDisposable
{
    private readonly string _root;

    public JobScriptSecurityTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "QueryPlusJobScriptSecurityTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void TryResolveContainedPath_PathStrictlyUnderRoot_Accepts()
    {
        var scriptPath = Path.Combine(_root, "backup.sh");
        File.WriteAllText(scriptPath, "#!/bin/bash\necho hi\n");

        var result = JobScriptSecurity.TryResolveContainedPath(_root, scriptPath, out var resolvedPath, out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
        resolvedPath.Should().Be(Path.GetFullPath(scriptPath));
    }

    [Fact]
    public void TryResolveContainedPath_ParentDirectoryTraversal_Rejects()
    {
        var subRoot = Directory.CreateDirectory(Path.Combine(_root, "allowed")).FullName;
        var outsideFile = Path.Combine(_root, "outside.sh");
        File.WriteAllText(outsideFile, "#!/bin/bash\necho hi\n");
        var traversalPath = Path.Combine(subRoot, "..", "outside.sh");

        var result = JobScriptSecurity.TryResolveContainedPath(subRoot, traversalPath, out var resolvedPath, out var error);

        result.Should().BeFalse();
        resolvedPath.Should().BeEmpty();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryResolveContainedPath_AbsolutePathEntirelyOutsideRoot_Rejects()
    {
        var allowedRoot = Directory.CreateDirectory(Path.Combine(_root, "allowed")).FullName;
        var elsewhere = Directory.CreateDirectory(Path.Combine(_root, "elsewhere")).FullName;
        var outsideFile = Path.Combine(elsewhere, "script.sh");
        File.WriteAllText(outsideFile, "#!/bin/bash\necho hi\n");

        var result = JobScriptSecurity.TryResolveContainedPath(allowedRoot, outsideFile, out var resolvedPath, out var error);

        result.Should().BeFalse();
        resolvedPath.Should().BeEmpty();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryResolveContainedPath_SymlinkResolvingOutsideRoot_Rejects()
    {
        var allowedRoot = Directory.CreateDirectory(Path.Combine(_root, "allowed")).FullName;
        var elsewhere = Directory.CreateDirectory(Path.Combine(_root, "elsewhere")).FullName;
        var targetFile = Path.Combine(elsewhere, "real-target.sh");
        File.WriteAllText(targetFile, "#!/bin/bash\necho hi\n");
        var linkPath = Path.Combine(allowedRoot, "link.sh");
        File.CreateSymbolicLink(linkPath, targetFile);

        var result = JobScriptSecurity.TryResolveContainedPath(allowedRoot, linkPath, out var resolvedPath, out var error);

        result.Should().BeFalse();
        resolvedPath.Should().BeEmpty();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryResolveContainedPath_TargetDoesNotExist_Rejects()
    {
        var missingPath = Path.Combine(_root, "does-not-exist.sh");

        var result = JobScriptSecurity.TryResolveContainedPath(_root, missingPath, out var resolvedPath, out var error);

        result.Should().BeFalse();
        resolvedPath.Should().BeEmpty();
        error.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolveContainedPath_AllowedRootNullOrEmpty_Rejects(string? allowedRoot)
    {
        var scriptPath = Path.Combine(_root, "backup.sh");
        File.WriteAllText(scriptPath, "#!/bin/bash\necho hi\n");

        var result = JobScriptSecurity.TryResolveContainedPath(allowedRoot!, scriptPath, out var resolvedPath, out var error);

        result.Should().BeFalse();
        resolvedPath.Should().BeEmpty();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ComputeSha256Async_FixedFixtureFile_IsStableAcrossCallsAndLooksLikeSha256()
    {
        var fixturePath = Path.Combine(_root, "fixture.sh");
        File.WriteAllText(fixturePath, "#!/bin/bash\necho \"stable content\"\n");

        var first = await JobScriptSecurity.ComputeSha256Async(fixturePath);
        var second = await JobScriptSecurity.ComputeSha256Async(fixturePath);

        first.Should().MatchRegex("^[0-9a-f]{64}$");
        second.Should().Be(first);
    }

    [Fact]
    public void HashMatches_IsCaseInsensitive()
    {
        var lower = "a3f1c2b9e4d5067890abcdef1234567890abcdef1234567890abcdef123456";
        var upper = lower.ToUpperInvariant();

        JobScriptSecurity.HashMatches(lower, upper).Should().BeTrue();
    }

    [Fact]
    public void HashMatches_DifferentHashes_ReturnsFalse()
    {
        var expected = "a3f1c2b9e4d5067890abcdef1234567890abcdef1234567890abcdef123456";
        var actual = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";

        JobScriptSecurity.HashMatches(expected, actual).Should().BeFalse();
    }

    // This is the last line of defense before a RunAsUser value is embedded into a root-owned
    // cron.d line invoking "systemd-run --uid=<value>" - it must reject "root" unconditionally,
    // independent of the eligible-user catalog or any config flag, since those can in principle
    // be misconfigured or fail to load.
    [Theory]
    [InlineData("root")]
    [InlineData("ROOT")]
    [InlineData("RoOt")]
    public void IsValidRunAsUser_Root_RejectsRegardlessOfCase(string value)
    {
        JobScriptSecurity.IsValidRunAsUser(value).Should().BeFalse();
    }

    [Fact]
    public void IsValidRunAsUser_OrdinarySystemAccountName_Accepts()
    {
        JobScriptSecurity.IsValidRunAsUser("svc-jobs").Should().BeTrue();
    }
}
