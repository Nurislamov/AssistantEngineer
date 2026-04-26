namespace AssistantEngineer.Api.Configuration;

internal static class ApiPipelineConfiguration
{
    public static WebApplication UseApiPipeline(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseRequestTimeouts();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}