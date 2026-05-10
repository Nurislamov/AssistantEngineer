namespace AssistantEngineer.Tests.Architecture;

public class EngineeringCalculationScenarioRunnerP1RequestValidatorGuardTests
{
    private static readonly string RunnerPath = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Services",
        "Calculations",
        "EngineeringCalculationScenarioRunner.cs");

    private static readonly string ValidatorPath = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Services",
        "Calculations",
        "ScenarioExecution",
        "EngineeringCalculationScenarioRequestValidator.cs");

    private static readonly string ApiPresentationRegistrationPath = Path.Combine(
        TestPaths.RepoRoot,
        "src",
        "Backend",
        "AssistantEngineer.Api",
        "Configuration",
        "ApiPresentationRegistration.cs");

    [Fact]
    public void ScenarioRunnerDelegatesRequestValidationAndDiagnosticNormalization()
    {
        var runnerText = File.ReadAllText(RunnerPath);

        Assert.Contains("IEngineeringCalculationScenarioRequestValidator", runnerText, StringComparison.Ordinal);
        Assert.Contains("_requestValidator.Validate(request)", runnerText, StringComparison.Ordinal);
        Assert.Contains("_requestValidator.SortAndDistinct", runnerText, StringComparison.Ordinal);
        Assert.Contains("_requestValidator.HasErrors(diagnostics)", runnerText, StringComparison.Ordinal);

        Assert.DoesNotContain("private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> ValidateScenarioRequest", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private static bool IsError", runnerText, StringComparison.Ordinal);
    }

    [Fact]
    public void RequestValidatorOwnsScenarioPreflightRulesAndDiagnosticPolicy()
    {
        var validatorText = File.ReadAllText(ValidatorPath);

        Assert.Contains("public IReadOnlyList<EngineeringWorkflowDiagnosticDto> Validate", validatorText, StringComparison.Ordinal);
        Assert.Contains("SCENARIO_ID_MISSING", validatorText, StringComparison.Ordinal);
        Assert.Contains("SCENARIO_PROJECT_ID_INVALID", validatorText, StringComparison.Ordinal);
        Assert.Contains("SCENARIO_ZONES_REQUIRED", validatorText, StringComparison.Ordinal);
        Assert.Contains("public IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinct", validatorText, StringComparison.Ordinal);
        Assert.Contains("public bool HasErrors", validatorText, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiPresentationRegistersRequestValidatorAsScopedService()
    {
        var registrationText = File.ReadAllText(ApiPresentationRegistrationPath);

        Assert.Contains(
            "services.AddScoped<IEngineeringCalculationScenarioRequestValidator, EngineeringCalculationScenarioRequestValidator>();",
            registrationText,
            StringComparison.Ordinal);
    }
}