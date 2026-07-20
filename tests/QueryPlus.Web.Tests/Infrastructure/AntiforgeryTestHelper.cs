using System.Text.RegularExpressions;

namespace QueryPlus.Web.Tests.Infrastructure;

/// <summary>
/// Extracts antiforgery tokens from rendered HTML for integration POSTs.
/// Prefers the form field (ASP.NET default); falls back to the layout meta tag used by HTMX.
/// </summary>
public static partial class AntiforgeryTestHelper
{
    public static async Task<string> GetRequestTokenAsync(HttpClient client, string path = "/")
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        return ExtractToken(html)
               ?? throw new InvalidOperationException(
                   $"Could not find antiforgery token on GET {path}.");
    }

    public static string? ExtractToken(string html)
    {
        var form = FormTokenRegex().Match(html);
        if (form.Success)
        {
            return form.Groups[1].Value;
        }

        var meta = MetaTokenRegex().Match(html);
        return meta.Success ? meta.Groups[1].Value : null;
    }

    /// <summary>
    /// Builds a form POST that includes the antiforgery token (form field + HTMX header).
    /// </summary>
    public static HttpRequestMessage CreateFormPost(
        string url,
        string antiforgeryToken,
        IEnumerable<KeyValuePair<string, string>> fields)
    {
        var pairs = fields
            .Append(new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken))
            .ToList();

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            // FormUrlEncodedContent sets application/x-www-form-urlencoded (+ charset).
            Content = new FormUrlEncodedContent(pairs)
        };
        // Same header ClientApp HTMX bridge sends from meta[name=csrf-token].
        // ASP.NET validates the form field by default; header is belt-and-suspenders when configured.
        request.Headers.TryAddWithoutValidation("RequestVerificationToken", antiforgeryToken);
        return request;
    }

    [GeneratedRegex(
        """name=["']__RequestVerificationToken["'][^>]*value=["']([^"']+)["']""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FormTokenRegex();

    [GeneratedRegex(
        """name=["']csrf-token["']\s+content=["']([^"']+)["']""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MetaTokenRegex();
}
