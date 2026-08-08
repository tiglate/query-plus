using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using QueryPlus.Api.DependencyInjection;
using QueryPlus.Api.Hosting;
using QueryPlus.Api.Json;
using QueryPlus.Api.ProblemDetails;
using QueryPlus.Api.Security;
using QueryPlus.Application.DependencyInjection;
using QueryPlus.Infrastructure.DependencyInjection;

const string CorsPolicyName = "QueryPlusSpa";

EnvFileLoader.LoadFromAncestors(Directory.GetCurrentDirectory());
EnvFileLoader.LoadFromAncestors(AppContext.BaseDirectory);
await OpenBaoSecretLoader.LoadFromEnvironmentAsync();
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices();
builder.Services.AddApiRateLimiting();
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "QueryPlus.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (allowedOrigins.Length == 0)
    {
        if (builder.Environment.IsDevelopment())
        {
            allowedOrigins =
            [
                "http://localhost:5132",
                "https://localhost:7192",
                "http://localhost:5173",
                "http://localhost:5000"
            ];
        }
        else
        {
            throw new InvalidOperationException(
                "CORS origins ('Cors:AllowedOrigins') must be explicitly configured in non-Development environments.");
        }
    }

    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Location", "X-CSRF-TOKEN");
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddScoped<AutoValidateAntiforgeryAuthorizationFilter>();
builder.Services.AddControllers(options => options.Filters.AddService<AutoValidateAntiforgeryAuthorizationFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringTrimConverter());
    });
var app = builder.Build();
await app.SeedDemoDataAsync();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapGet("/login",
    [AllowAnonymous](HttpContext context, string? returnUrl) => Results.Challenge(
        new AuthenticationProperties { RedirectUri = IsLocalUrl(returnUrl) ? returnUrl : "/" },
        [OpenIdConnectDefaults.AuthenticationScheme]));
app.MapControllers();
app.MapFallbackToFile("index.html").AllowAnonymous();
app.Run();

static bool IsLocalUrl(string? url)
{
    if (string.IsNullOrWhiteSpace(url))
    {
        return false;
    }

    if (url[0] != '/' || (url.Length > 1 && (url[1] == '/' || url[1] == '\\')))
    {
        return false;
    }

    return Uri.TryCreate(url, UriKind.Relative, out _);
}

public partial class Program;
