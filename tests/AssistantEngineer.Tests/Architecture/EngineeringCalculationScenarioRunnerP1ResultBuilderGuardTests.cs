namespace AssistantEngineer.Tests.Architecture;

public class EngineeringCalculationScenarioRunnerP1ResultBuilderGuardTests
{
    private static readonly string RunnerPath = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Services",
        "Calculations",
        "EngineeringCalculationScenarioRunner.cs");

    private static readonly string ResultBuilderPath = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Services",
        "Calculations",
        "ScenarioExecution",
        "EngineeringCalculationScenarioResultBuilder.cs");

    private static readonly string ApiPresentationRegistrationPath = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Configuration",
        "ApiPresentationRegistration.cs");

    [Fact]
    public void ScenarioRunnerDelegatesTraceReportAndResultFinalizationToResultBuilder()
    {
        var runnerText = File.ReadAllText(RunnerPath);

        Assert.Contains("IEngineeringCalculationScenarioResultBuilder", runnerText, StringComparison.Ordinal);
        Assert.Contains("_resultBuilder.BuildScenarioResult", runnerText, StringComparison.Ordinal);
        Assert.Contains("_resultBuilder.BuildTrace", runnerText, StringComparison.Ordinal);
        Assert.Contains("_resultBuilder.FindModuleSummary", runnerText, StringComparison.Ordinal);

        Assert.DoesNotContain("private CalculationTraceDocument BuildTrace", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private EngineeringCalculationScenarioResultDto BuildScenarioResult", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("ICalculationTraceBuilder _traceBuilder", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("IEngineeringReportBuilder _reportBuilder", runnerText, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultBuilderOwnsTraceReportStatusAndSummaryFinalization()
    {
        var builderText = File.ReadAllText(ResultBuilderPath);

        Assert.Contains("public CalculationTraceDocument BuildTrace", builderText, StringComparison.Ordinal);
        Assert.Contains("public EngineeringCalculationScenarioResultDto BuildScenarioResult", builderText, StringComparison.Ordinal);
        Assert.Contains("private static EngineeringCalculationExecutionStatus DetermineStatus", builderText, StringComparison.Ordinal);
        Assert.Contains("public string FindModuleSummary", builderText, StringComparison.Ordinal);
        Assert.Contains("IEngineeringReportBuilder", builderText, StringComparison.Ordinal);
        Assert.Contains("ICalculationTraceBuilder", builderText, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiPresentationRegistersScenarioResultBuilder()
    {
        var registrationText = File.ReadAllText(ApiPresentationRegistrationPath);

        Assert.Contains(
            "services.AddScoped<IEngineeringCalculationScenarioResultBuilder, EngineeringCalculationScenarioResultBuilder>()",
            registrationText,
            StringComparison.Ordinal);
    }
}