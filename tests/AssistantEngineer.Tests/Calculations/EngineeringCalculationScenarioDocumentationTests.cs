using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationScenarioDocumentationTests
{
    [Fact]
    public void ScenarioRunnerDocumentationExistsAndListsExecutionModes()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "engineering-calculation-scenario-runner.md");

        Assert.True(File.Exists(path), $"Scenario runner documentation must exist: {path}");
        var content = File.ReadAllText(path);

        Assert.Contains("ValidateOnly", content, StringComparison.Ordinal);
        Assert.Contains("PrepareOnly", content, StringComparison.Ordinal);
        Assert.Contains("ExecuteAvailableModules", content, StringComparison.Ordinal);
        Assert.Contains("ExecuteFullRequired", content, StringComparison.Ordinal);
        Assert.Contains("DryRun", content, StringComparison.Ordinal);
        Assert.Contains("POST /api/v1/engineering-workflow/run-calculation", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerDocumentationContainsRequiredLimitationsAndNonClaims()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "engineering-calculation-scenario-runner.md");

        var content = File.ReadAllText(path);

        Assert.Contains("foundation runner executes only modules with available structured inputs", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no hidden external weather calls", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no fake calculation success", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a compliance certificate", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not external validation evidence", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no full standard compliance claim", content, StringComparison.OrdinalIgnoreCase);
    }
}
