using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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
        app.UseCors(ApiHardeningRegistration.DefaultCorsPolicyName);
        app.UseRequestTimeouts();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapHealthChecks("/health",
                new HealthCheckOptions
                {
                    Predicate = registration => registration.Tags.Contains(ApiHardeningRegistration.LivenessTag, StringComparer.Ordinal)
                })
            .AllowAnonymous();

        app.MapHealthChecks("/ready",
                new HealthCheckOptions
                {
                    Predicate = registration => registration.Tags.Contains(ApiHardeningRegistration.ReadinessTag, StringComparer.Ordinal)
                })
            .AllowAnonymous();

        app.MapControllers();

        return app;
    }
}
