using System.Text;
using System.Text.RegularExpressions;

namespace QueryPlus.Application.Common;

/// <summary>
/// Defense-in-depth sanitization for user-supplied parameter values.
/// Stored procedure execution already uses ADO.NET parameters (no dynamic SQL
/// concatenation in the app). FreeText values are still sanitized so that
/// common LIKE wildcards and control characters cannot be abused when a
/// procedure pattern is <c>LIKE '%' + @value + '%'</c>.
/// </summary>
public static partial class ParameterSecurity
{
    /// <summary>Max length for free-text / combo string values after trim.</summary>
    public const int MaxFreeTextLength = 200;

    /// <summary>
    /// Characters that act as wildcards / character-class openers in SQL Server LIKE.
    /// </summary>
    private static readonly char[] LikeMetaCharacters = ['%', '_', '['];

    /// <summary>
    /// Sanitizes free-text user input for safe use as a SQL parameter value.
    /// </summary>
    /// <exception cref="FormatException">When the value is unsafe or malformed.</exception>
    public static string SanitizeFreeText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Reject embedded nulls (classic string-termination / driver edge cases).
        if (value.Contains('\0'))
        {
            throw new FormatException("Free text must not contain null characters.");
        }

        // Normalize and strip other C0 control characters (keep ordinary whitespace).
        var cleaned = StripDangerousControlCharacters(value).Trim();

        if (cleaned.Length == 0)
        {
            return cleaned;
        }

        if (cleaned.Length > MaxFreeTextLength)
        {
            throw new FormatException(
                $"Free text must be at most {MaxFreeTextLength} characters.");
        }

        // Standalone / pure-wildcard input is a common way to dump all LIKE matches.
        if (IsOnlyLikeMetaOrWhitespace(cleaned))
        {
            throw new FormatException(
                "Free text cannot consist only of SQL wildcard characters (%, _, [).");
        }

        // Escape LIKE metacharacters so user input is treated literally when
        // procedures use patterns such as LIKE '%' + @Name + '%'.
        return EscapeLikeMetacharacters(cleaned);
    }

    /// <summary>
    /// Escapes SQL Server LIKE metacharacters using character classes
    /// (<c>%</c> → <c>[%]</c>, <c>_</c> → <c>[_]</c>, <c>[</c> → <c>[[]</c>).
    /// </summary>
    public static string EscapeLikeMetacharacters(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '[':
                    sb.Append("[[]");
                    break;
                case '%':
                    sb.Append("[%]");
                    break;
                case '_':
                    sb.Append("[_]");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns true when the string is composed only of LIKE meta characters and/or whitespace.
    /// </summary>
    public static bool IsOnlyLikeMetaOrWhitespace(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
            {
                continue;
            }

            if (Array.IndexOf(LikeMetaCharacters, ch) < 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Detects obvious SQL injection fragments that should never appear in free text
    /// for this application (defense in depth; parameterization already prevents execution).
    /// </summary>
    public static bool ContainsSuspiciousSqlFragment(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return SuspiciousSqlRegex().IsMatch(value);
    }

    /// <summary>
    /// Applies free-text policy: control chars, length, wildcards, and suspicious fragments.
    /// </summary>
    public static string SanitizeAndValidateFreeText(string value)
    {
        var sanitized = SanitizeFreeText(value);

        // Check the original trimmed input (before escape) for injection phrases.
        var originalTrimmed = StripDangerousControlCharacters(value).Trim();
        if (ContainsSuspiciousSqlFragment(originalTrimmed))
        {
            throw new FormatException(
                "Free text contains disallowed SQL-like content.");
        }

        return sanitized;
    }

    public static string StripDangerousControlCharacters(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            // Allow tab (9), LF (10), CR (13); drop other C0 controls and DEL.
            if (ch is '\t' or '\n' or '\r')
            {
                sb.Append(ch);
                continue;
            }

            if (ch < 32 || ch == 127)
            {
                continue;
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Validates a parameter name used with DynamicParameters (letters/digits/underscore only).
    /// </summary>
    public static void EnsureSafeParameterName(string name)
    {
        var normalized = SqlIdentifier.NormalizeParameterName(name);
        var bare = normalized.TrimStart('@');
        if (!SqlIdentifier.IsValidSegment(bare))
        {
            throw new ArgumentException($"Invalid parameter name '{name}'.", nameof(name));
        }
    }

    // Comments, batch separators, UNION, OR 1=1 style fragments, xp_cmdshell, etc.
    [GeneratedRegex(
        @"('(\s|--)|;\s*|/\*|\*/|\b(UNION|SELECT|INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|EXEC|EXECUTE|XP_|SP_)\b|\bOR\b\s+['\d]|\bAND\b\s+['\d])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex SuspiciousSqlRegex();
}
