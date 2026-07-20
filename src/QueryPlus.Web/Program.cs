using QueryPlus.Application.DependencyInjection;
using QueryPlus.Infrastructure.DependencyInjection;
using QueryPlus.Web.DependencyInjection;
using QueryPlus.Web.Hosting;

// Composition root only — registration and pipeline live in focused extension classes.
// Controllers: Controllers/ (+ Controllers/Admin/)
// Views: Views/ (Razor views for MVC)

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebServices();
builder.Services.AddWebMvc();
builder.Services.AddWebLocalization();
builder.Services.AddWebAuthentication(builder.Configuration);

var app = builder.Build();

// Ensure schema + demo tables/procedures/catalog exist before serving traffic.
await app.SeedDemoDataAsync();

app.UseWebPipeline();
app.MapWebEndpoints();

app.Run();

// Expose for WebApplicationFactory integration tests.
public partial class Program;
