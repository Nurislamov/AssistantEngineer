using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Api;

public class EngineeringWorkflowApiDocumentationTests
{
    [Fact]
    public void EngineeringWorkflowApiDocumentationExistsAndListsEndpoints()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-workflow-api.md");

        Assert.True(File.Exists(path), $"Engineering workflow API documentation must exist: {path}");

        var content = File.ReadAllText(path);

        Assert.Contains("GET /api/v1/engineering-workflow/{projectId}/state", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/validate", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/prepare-calculation", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/run-calculation", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/jobs", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/jobs/{jobId}", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/jobs/{jobId}/events", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/jobs/{jobId}/cancel", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/{projectId}/jobs", content, StringComparison.Ordinal);
        Assert.Contains("page", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pageSize", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST /api/v1/engineering-workflow/trace-preview", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/report", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/report/export/json", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/report/export/markdown", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/{projectId}/scenarios", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/scenarios/{scenarioId}", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts/{artifactKind}", content, StringComparison.Ordinal);
        Assert.Contains("Idempotency-Key", content, StringComparison.Ordinal);
    }

    [Fact]
    public void EngineeringWorkflowApiDocumentationIncludesRequiredLimitationsAndNonClaims()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-workflow-api.md");

        var content = File.ReadAllText(path);

        Assert.Contains("may prepare or preview calculations without executing full production scenario", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a compliance certificate", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reports summarize current internal engineering calculations only", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("trace explains internal calculation chain only", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no external validation evidence", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no full standard compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("persistence foundation may be in-memory", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("provider model", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("idempotency", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("409", content, StringComparison.OrdinalIgnoreCase);
    }
}
