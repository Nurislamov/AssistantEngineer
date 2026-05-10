namespace AssistantEngineer.Tests.Architecture;

public class EngineeringCalculationScenarioRunnerP1StepExtractionGuardTests
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
    public void ScenarioRunnerDelegatesWeatherSolarAndVentilationSteps()
    {
        var runnerText = File.ReadAllText(RunnerPath);

        Assert.Contains("IEngineeringCalculationWeatherSolarScenarioStep", runnerText, StringComparison.Ordinal);
        Assert.Contains("IEngineeringCalculationVentilationScenarioStep", runnerText, StringComparison.Ordinal);
        Assert.Contains("() => _weatherSolarScenarioStep.Execute(request)", runnerText, StringComparison.Ordinal);
        Assert.Contains("() => _ventilationScenarioStep.Execute(request)", runnerText, StringComparison.Ordinal);

        Assert.DoesNotContain("Weather and solar readiness data is unavailable.", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("Structured natural ventilation hourly input is not available", runnerText, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioStepServicesOwnWeatherSolarAndVentilationReadinessRules()
    {
        var weatherStepText = File.ReadAllText(Path.Combine(
            ScenarioExecutionDirectory,
            "EngineeringCalculationWeatherSolarScenarioStep.cs"));
        var ventilationStepText = File.ReadAllText(Path.Combine(
            ScenarioExecutionDirectory,
            "EngineeringCalculationVentilationScenarioStep.cs"));

        Assert.Contains("Weather and solar readiness data is unavailable.", weatherStepText, StringComparison.Ordinal);
        Assert.Contains("WorkflowState.WeatherSolarSettings", weatherStepText, StringComparison.Ordinal);
        Assert.Contains("No natural ventilation openings are configured.", ventilationStepText, StringComparison.Ordinal);
        Assert.Contains("Structured natural ventilation hourly input is not available", ventilationStepText, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiPresentationRegistersScenarioStepsAsScopedServices()
    {
        var registrationText = File.ReadAllText(ApiPresentationRegistrationPath);

        Assert.Contains(
            "services.AddScoped<IEngineeringCalculationWeatherSolarScenarioStep, EngineeringCalculationWeatherSolarScenarioStep>();",
            registrationText,
            StringComparison.Ordinal);
        Assert.Contains(
            "services.AddScoped<IEngineeringCalculationVentilationScenarioStep, EngineeringCalculationVentilationScenarioStep>();",
            registrationText,
            StringComparison.Ordinal);
    }
}