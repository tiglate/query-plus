using System.Net.Http.Json;
using System.Text.Json;

namespace QueryPlus.Api.Tests.Infrastructure;

public static class AntiforgeryApiHelper
{
    public const string CsrfCookieName = "QueryPlus.Csrf";
    public const string CsrfHeaderName = "X-CSRF-TOKEN";
    public const string CsrfFormFieldName = "__RequestVerificationToken";
    public const string CsrfEndpoint = "/api/auth/csrf";

    public static async Task<string> GetTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync(CsrfEndpoint);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!payload.TryGetProperty("token", out var tokenElement))
        {
            throw new InvalidOperationException("CSRF response did not contain 'token'.");
        }

        return tokenElement.GetString()
               ?? throw new InvalidOperationException("CSRF token was null.");
    }

    public static HttpRequestMessage CreateJsonPost(
        string url,
        string antiforgeryToken,
        HttpContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.TryAddWithoutValidation(CsrfHeaderName, antiforgeryToken);
        return request;
    }

    public static HttpRequestMessage CreateFormPost(
        string url,
        string antiforgeryToken)
    {
        return new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>(CsrfFormFieldName, antiforgeryToken)
                })
        };
    }
}
