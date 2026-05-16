namespace AssistantEngineer.Api.Security.RateLimiting;

public sealed class DefaultEndpointRateLimitCategoryResolver : IEndpointRateLimitCategoryResolver
{
    public string ResolveCategory(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var normalizedPath = path.ToLowerInvariant();
        var method = httpContext.Request.Method;

        if (normalizedPath.Contains("workflow", StringComparison.Ordinal) &&
            (normalizedPath.Contains("/run", StringComparison.Ordinal) ||
             normalizedPath.Contains("/execute", StringComparison.Ordinal)))
        {
            return EndpointRateLimitCategories.WorkflowExecute;
        }

        if (normalizedPath.Contains("workflow", StringComparison.Ordinal))
        {
            return EndpointRateLimitCategories.WorkflowRead;
        }

        if (normalizedPath.Contains("/calculations", StringComparison.Ordinal))
        {
            return EndpointRateLimitCategories.CalculationRun;
        }

        if (normalizedPath.Contains("/reports", StringComparison.Ordinal))
        {
            return EndpointRateLimitCategories.ReportGenerate;
        }

        if (normalizedPath.Contains("/artifacts", StringComparison.Ordinal))
        {
            return HttpMethods.IsGet(method)
                ? EndpointRateLimitCategories.ArtifactRead
                : EndpointRateLimitCategories.ArtifactWrite;
        }

        if (normalizedPath.Contains("/reference", StringComparison.Ordinal))
        {
            return EndpointRateLimitCategories.ReferenceData;
        }

        if (normalizedPath.Contains("/projects", StringComparison.Ordinal))
        {
            return HttpMethods.IsGet(method)
                ? EndpointRateLimitCategories.ProjectRead
                : EndpointRateLimitCategories.ProjectWrite;
        }

        if (normalizedPath.Contains("/buildings", StringComparison.Ordinal))
        {
            return HttpMethods.IsGet(method)
                ? EndpointRateLimitCategories.BuildingRead
                : EndpointRateLimitCategories.BuildingWrite;
        }

        if (normalizedPath.Contains("/admin", StringComparison.Ordinal) ||
            normalizedPath.Contains("/development", StringComparison.Ordinal))
        {
            return EndpointRateLimitCategories.Administration;
        }

        return EndpointRateLimitCategories.PublicRead;
    }
}
