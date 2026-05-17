using AssistantEngineer.Api.Security.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace AssistantEngineer.Tests.Api;

public sealed class EndpointRateLimitCategoryResolverTests
{
    private readonly IEndpointRateLimitCategoryResolver _resolver = new DefaultEndpointRateLimitCategoryResolver();

    [Fact]
    public void WorkflowExecutePath_ResolvesToWorkflowExecute()
    {
        var context = CreateContext("POST", "/api/v1/engineering-workflow/run-calculation");

        var category = _resolver.ResolveCategory(context);

        Assert.Equal(EndpointRateLimitCategories.WorkflowExecute, category);
    }

    [Fact]
    public void CalculationsPath_ResolvesToCalculationRun()
    {
        var context = CreateContext("POST", "/api/v1/calculations/room/heating");

        var category = _resolver.ResolveCategory(context);

        Assert.Equal(EndpointRateLimitCategories.CalculationRun, category);
    }

    [Fact]
    public void ReportsPath_ResolvesToReportGenerate()
    {
        var context = CreateContext("POST", "/api/v1/reports/export");

        var category = _resolver.ResolveCategory(context);

        Assert.Equal(EndpointRateLimitCategories.ReportGenerate, category);
    }

    [Fact]
    public void ArtifactsGetPath_ResolvesToArtifactRead()
    {
        var context = CreateContext("GET", "/api/v1/engineering-workflow/scenarios/abc/artifacts");

        var category = _resolver.ResolveCategory(context);

        Assert.Equal(EndpointRateLimitCategories.ArtifactRead, category);
    }

    [Fact]
    public void ArtifactsWritePath_ResolvesToArtifactWrite()
    {
        var context = CreateContext("POST", "/api/v1/engineering-workflow/scenarios/abc/artifacts");

        var category = _resolver.ResolveCategory(context);

        Assert.Equal(EndpointRateLimitCategories.ArtifactWrite, category);
    }

    [Fact]
    public void WorkflowReadStatePath_ResolvesToWorkflowRead()
    {
        var context = CreateContext("GET", "/api/v1/engineering-workflow/1/state");

        var category = _resolver.ResolveCategory(context);

        Assert.Equal(EndpointRateLimitCategories.WorkflowRead, category);
    }

    [Fact]
    public void WorkflowReadScenarioPath_ResolvesToWorkflowRead()
    {
        var context = CreateContext("GET", "/api/v1/engineering-workflow/scenarios/sample");

        var category = _resolver.ResolveCategory(context);

        Assert.Equal(EndpointRateLimitCategories.WorkflowRead, category);
    }

    [Fact]
    public void ReferencePath_ResolvesToReferenceData()
    {
        var context = CreateContext("GET", "/api/v1/reference-data/climate-zones");

        var category = _resolver.ResolveCategory(context);

        Assert.Equal(EndpointRateLimitCategories.ReferenceData, category);
    }

    [Fact]
    public void UnknownPath_ResolvesToPublicRead()
    {
        var context = CreateContext("GET", "/api/v1/ping");

        var category = _resolver.ResolveCategory(context);

        Assert.Equal(EndpointRateLimitCategories.PublicRead, category);
    }

    private static HttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        return context;
    }
}
