namespace AssistantEngineer.Tests.Architecture;

public class EngineeringCalculationScenarioRunnerP1GroundDhwStepGuardTests
{
    private static readonly string RunnerPath = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Services",
        "Calculations",
        "EngineeringCalculationScenarioRunner.cs");

    private static readonly string ScenarioExecutionDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Services",
        "Calculations",
        "ScenarioExecution");

    private static readonly string ApiPresentationRegistrationPath = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Configuration",
        "ApiPresentationRegistration.cs");

    [Fact]
    public void ScenarioRunnerDelegatesGroundAndDomesticHotWaterSteps()
    {
        var runnerText = File.ReadAllText(RunnerPath);

        Assert.Contains("IEngineeringCalculationGroundScenarioStep", runnerText, StringComparison.Ordinal);
        Assert.Contains("IEngineeringCalculationDomesticHotWaterScenarioStep", runnerText, StringComparison.Ordinal);
        Assert.Contains("() => _groundScenarioStep.Execute(request)", runnerText, StringComparison.Ordinal);
        Assert.Contains("_domesticHotWaterScenarioStep.Execute(request)", runnerText, StringComparison.Ordinal);

        Assert.DoesNotContain("IDomesticHotWaterSystemLoadCalculator", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("No ground boundaries are configured.", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("DHW annual useful demand is not provided", runnerText, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioStepServicesOwnGroundAndDomesticHotWaterReadinessRules()
    {
        var groundStepText = File.ReadAllText(Path.Combine(
            ScenarioExecutionDirectory,
            "EngineeringCalculationGroundScenarioStep.cs"));
        var dhwStepText = File.ReadAllText(Path.Combine(
            ScenarioExecutionDirectory,
            "EngineeringCalculationDomesticHotWaterScenarioStep.cs"));

        Assert.Contains("No ground boundaries are configured.", groundStepText, StringComparison.Ordinal);
        Assert.Contains("Structured ground boundary geometry", groundStepText, StringComparison.Ordinal);
        Assert.Contains("DHW annual useful demand is not provided", dhwStepText, StringComparison.Ordinal);
        Assert.Contains("IDomesticHotWaterSystemLoadCalculator", dhwStepText, StringComparison.Ordinal);
        Assert.Contains("DomesticHotWaterLossDefinition", dhwStepText, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiPresentationRegistersGroundAndDomesticHotWaterStepsAsScopedServices()
    {
        var registrationText = File.ReadAllText(ApiPresentationRegistrationPath);

        Assert.Contains(
            "services.AddScoped<IEngineeringCalculationGroundScenarioStep, EngineeringCalculationGroundScenarioStep>();",
            registrationText,
            StringComparison.Ordinal);
        Assert.Contains(
            "services.AddScoped<IEngineeringCalculationDomesticHotWaterScenarioStep, EngineeringCalculationDomesticHotWaterScenarioStep>();",
            registrationText,
            StringComparison.Ordinal);
    }
}