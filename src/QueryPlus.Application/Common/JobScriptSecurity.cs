using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace QueryPlus.Application.Common;

/// <summary>
/// Validates job script paths against an admin-configured allowlisted root directory, and
/// computes/compares the SHA-256 pinned at approval time. Used at three trust levels: save-time
/// (containment only), approval-time (containment + compute + pin), and execution-time in
/// QueryPlus.Runner (containment + recompute + compare - the actual defense against a script
/// being swapped after approval).
/// </summary>
public static partial class JobScriptSecurity
{
    /// <summary>
    /// Portable POSIX username charset (letters/digits/underscore/hyphen, must not start with a
    /// digit or hyphen, max 32 chars - matches useradd's default NAME_REGEX). RunAsUser is
    /// embedded, unescaped, into a raw /etc/cron.d text line that cron hands to a shell as root -
    /// this is the only thing standing between an attacker-controlled value and shell metacharacter
    /// injection there, so it is deliberately far stricter than "not empty, under 64 chars".
    /// </summary>
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_-]{0,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex RunAsUserRegex();

    /// <summary>
    /// "root" is excluded unconditionally here - not just filtered out of the eligible-accounts
    /// catalog - because this method is also CronSyncService's last line of defense before a value
    /// is embedded into a root-owned cron.d line invoking "systemd-run --uid=&lt;value&gt;". Every
    /// other layer that restricts RunAsUser (the eligible-user catalog, its validator check) can in
    /// principle be misconfigured or fail open; this cannot, since it never depends on external
    /// state (a catalog query, a config flag) succeeding.
    /// </summary>
    public static bool IsValidRunAsUser(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && RunAsUserRegex().IsMatch(value)
           && !string.Equals(value, "root", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves <paramref name="candidatePath"/> against <paramref name="allowedRoot"/>, rejecting
    /// traversal and symlinks whose target escapes the root even when the link's own path does not.
    /// </summary>
    public static bool TryResolveContainedPath(
        string allowedRoot,
        string candidatePath,
        out string resolvedPath,
        out string? error)
    {
        resolvedPath = string.Empty;

        if (string.IsNullOrWhiteSpace(allowedRoot))
        {
            error = "No script allowlist root is configured.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            error = "Script path is required.";
            return false;
        }

        string fullRoot;
        string fullCandidate;
        try
        {
            fullRoot = Path.GetFullPath(allowedRoot);
            fullCandidate = Path.GetFullPath(candidatePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = "Script path is malformed.";
            return false;
        }

        if (!IsContained(fullRoot, fullCandidate))
        {
            error = "Script path must resolve inside the configured allowlisted root directory.";
            return false;
        }

        if (!File.Exists(fullCandidate))
        {
            error = "Script file does not exist.";
            return false;
        }

        // A leaf-only symlink check is not sufficient: a symlinked *ancestor directory* anywhere
        // under the allowed root (e.g. allowedRoot/link -> /etc, with a real file at
        // allowedRoot/link/passwd) passes a purely lexical containment check and File.Exists,
        // even though the physical file lives outside the root entirely. Resolve every symlink in
        // the path - not just the leaf - and re-check containment against the real path.
        string realRoot;
        string realCandidate;
        try
        {
            realRoot = ResolveRealPath(fullRoot);
            realCandidate = ResolveRealPath(fullCandidate);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = "Script path could not be resolved (broken or excessively nested symlink).";
            return false;
        }

        if (!IsContained(realRoot, realCandidate))
        {
            error = "Script path resolves (via a symlinked directory) outside the configured allowlisted root directory.";
            return false;
        }

        error = null;
        resolvedPath = realCandidate;
        return true;
    }

    /// <summary>
    /// Resolves every symlink along <paramref name="fullPath"/> - including intermediate
    /// directory components, not just the leaf - by walking one path segment at a time and
    /// substituting each segment's target the moment it is itself found to be a symlink.
    /// </summary>
    private static string ResolveRealPath(string fullPath)
    {
        var root = Path.GetPathRoot(fullPath) ?? string.Empty;
        var relative = fullPath[root.Length..];
        var segments = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        var current = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (current.Length == 0)
        {
            current = Path.DirectorySeparatorChar.ToString();
        }

        foreach (var segment in segments)
        {
            current = Path.Combine(current, segment);
            var linkTarget = File.ResolveLinkTarget(current, returnFinalTarget: true);
            if (linkTarget is not null)
            {
                current = linkTarget.FullName;
            }
        }

        return current;
    }

    /// <summary>Lowercase hex SHA-256 of the file at <paramref name="resolvedPath"/>.</summary>
    public static async Task<string> ComputeSha256Async(
        string resolvedPath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(resolvedPath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }

    public static bool HashMatches(string expectedSha256, string actualSha256)
        => string.Equals(expectedSha256, actualSha256, StringComparison.OrdinalIgnoreCase);

    private static bool IsContained(string fullRoot, string fullCandidate)
    {
        var normalizedRoot = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                              + Path.DirectorySeparatorChar;
        var normalizedCandidate = fullCandidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                   + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(normalizedRoot, StringComparison.Ordinal);
    }
}
