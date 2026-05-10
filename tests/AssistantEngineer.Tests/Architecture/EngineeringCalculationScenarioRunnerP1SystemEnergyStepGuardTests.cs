namespace AssistantEngineer.Tests.Architecture;

public class EngineeringCalculationScenarioRunnerP1SystemEnergyStepGuardTests
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
    public void ScenarioRunnerDelegatesSystemEnergyStep()
    {
        var runnerText = File.ReadAllText(RunnerPath);

        Assert.Contains("IEngineeringCalculationSystemEnergyScenarioStep", runnerText, StringComparison.Ordinal);
        Assert.Contains("_systemEnergyScenarioStep.Execute(request, dhwFoundationSummary)", runnerText, StringComparison.Ordinal);

        Assert.DoesNotContain("ISystemEnergyFoundationCalculator", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildSystemEnergyLoads", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildDefaultSystemEnergyStages", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildDefaultSystemEnergyGenerators", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("System-energy useful loads are unavailable", runnerText, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioStepServiceOwnsSystemEnergyHandoffAndDefaultDefinitions()
    {
        var stepText = File.ReadAllText(Path.Combine(
            ScenarioExecutionDirectory,
            "EngineeringCalculationSystemEnergyScenarioStep.cs"));

        Assert.Contains("ISystemEnergyFoundationCalculator", stepText, StringComparison.Ordinal);
        Assert.Contains("BuildSystemEnergyLoads", stepText, StringComparison.Ordinal);
        Assert.Contains("BuildDefaultSystemEnergyStages", stepText, StringComparison.Ordinal);
        Assert.Contains("BuildDefaultSystemEnergyGenerators", stepText, StringComparison.Ordinal);
        Assert.Contains("System-energy useful loads are unavailable", stepText, StringComparison.Ordinal);
        Assert.Contains("DHW load was adapted from DHW foundation output profile", stepText, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiPresentationRegistersSystemEnergyStepAsScopedService()
    {
        var registrationText = File.ReadAllText(ApiPresentationRegistrationPath);

        Assert.Contains(
            "services.AddScoped<IEngineeringCalculationSystemEnergyScenarioStep, EngineeringCalculationSystemEnergyScenarioStep>();",
            registrationText,
            StringComparison.Ordinal);
    }
}