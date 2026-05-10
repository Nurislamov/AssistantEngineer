namespace AssistantEngineer.Tests.Architecture;

public class EngineeringCalculationScenarioRunnerP1ArchitectureGuardTests
{
    private static readonly string RunnerPath = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Services",
        "Calculations",
        "EngineeringCalculationScenarioRunner.cs");

    [Fact]
    public void ScenarioRunnerDelegatesModuleExecutionBookkeepingToDedicatedService()
    {
        var runnerText = File.ReadAllText(RunnerPath);

        Assert.Contains("IEngineeringCalculationScenarioModuleExecutor", runnerText, StringComparison.Ordinal);
        Assert.Contains("_moduleExecutor.Execute(", runnerText, StringComparison.Ordinal);
        Assert.Contains("_moduleExecutor.ExecuteAsync(", runnerText, StringComparison.Ordinal);
        Assert.Contains("_moduleExecutor.AddModuleOutcome(", runnerText, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerDoesNotOwnLowLevelModuleExecutionTypes()
    {
        var runnerText = File.ReadAllText(RunnerPath);

        Assert.DoesNotContain("private sealed class ModuleExecution", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private sealed record ModuleRunOutcome", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static ModuleRunOutcome ExecuteModule", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("using System.Diagnostics;", runnerText, StringComparison.Ordinal);
    }
}