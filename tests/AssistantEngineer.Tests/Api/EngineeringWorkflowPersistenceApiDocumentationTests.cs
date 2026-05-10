using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Api;

public class EngineeringWorkflowPersistenceApiDocumentationTests
{
    [Fact]
    public void WorkflowPersistenceDocumentationExistsAndListsPersistenceEndpoints()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-workflow-persistence.md");

        Assert.True(File.Exists(path), $"Workflow persistence documentation must exist: {path}");
        var content = File.ReadAllText(path);

        Assert.Contains("GET /api/v1/engineering-workflow/{projectId}/state", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/{projectId}/scenarios", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/scenarios/{scenarioId}", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts/{artifactKind}", content, StringComparison.Ordinal);
        Assert.Contains("Provider", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("InMemory", content, StringComparison.Ordinal);
        Assert.Contains("SQLite", content, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowPersistenceDocumentationContainsRequiredLimitationsAndNonClaims()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-workflow-persistence.md");

        var content = File.ReadAllText(path);

        Assert.Contains("persistence may be in-memory", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not validate calculation correctness", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("artifacts summarize internal engineering calculations only", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a compliance certificate", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not external validation evidence", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no full standard compliance claim", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DurablePersistenceDocumentationExistsAndDescribesProviderConfiguration()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-workflow-durable-persistence.md");

        Assert.True(File.Exists(path), $"Durable persistence documentation must exist: {path}");
        var content = File.ReadAllText(path);

        Assert.Contains("Provider", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EngineeringWorkflowPersistence", content, StringComparison.Ordinal);
        Assert.Contains("InMemory", content, StringComparison.Ordinal);
        Assert.Contains("SQLite", content, StringComparison.Ordinal);
        Assert.Contains("EnsureCreatedOnStartup", content, StringComparison.Ordinal);
        Assert.Contains("not a compliance certificate", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no full standard compliance claim", content, StringComparison.OrdinalIgnoreCase);
    }
}
