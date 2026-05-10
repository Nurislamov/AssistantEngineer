using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Api;

public class EngineeringCalculationJobsApiDocumentationTests
{
    [Fact]
    public void EngineeringCalculationJobsDocumentationExistsAndListsEndpoints()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-calculation-jobs.md");

        Assert.True(File.Exists(path), $"Engineering calculation jobs documentation must exist: {path}");
        var content = File.ReadAllText(path);

        Assert.Contains("POST /api/v1/engineering-workflow/jobs", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/jobs/{jobId}", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/jobs/{jobId}/events", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/jobs/{jobId}/cancel", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/{projectId}/jobs", content, StringComparison.Ordinal);
    }

    [Fact]
    public void EngineeringCalculationJobsDocumentationContainsLimitationsAndNonClaims()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-calculation-jobs.md");

        var content = File.ReadAllText(path);

        Assert.Contains("foundation job queue is not distributed", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no production-grade background worker", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no multi-node coordination", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a compliance certificate", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no external validation evidence", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no full standard compliance claim", content, StringComparison.OrdinalIgnoreCase);
    }
}
