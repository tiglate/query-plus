namespace QueryPlus.Web.Hosting;

/// <summary>
/// HTTP middleware pipeline and endpoint mapping (order matters).
/// </summary>
public static class ApplicationPipelineExtensions
{
    public static WebApplication UseWebPipeline(this WebApplication app)
    {
        // Friendly status pages (404, 500, 505, …). Re-execute keeps the real status code.
        app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        else
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapWebEndpoints(this WebApplication app)
    {
        app.MapStaticAssets();
        app.MapControllers();
        return app;
    }
}
