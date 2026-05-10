using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class ScenarioModuleExecution
{
    public EngineeringCalculationModuleExecutionStatus Status { get; }
    public IReadOnlyList<EngineeringCalculationModuleValueDto> Values { get; }
    public IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics { get; }
    public IReadOnlyList<string> Assumptions { get; }
    public IReadOnlyList<string> Warnings { get; }
    public string SourceServiceName { get; }

    private ScenarioModuleExecution(
        EngineeringCalculationModuleExecutionStatus status,
        IReadOnlyList<EngineeringCalculationModuleValueDto> values,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> warnings,
        string sourceServiceName)
    {
        Status = status;
        Values = values;
        Diagnostics = diagnostics;
        Assumptions = assumptions;
        Warnings = warnings;
        SourceServiceName = sourceServiceName;
    }

    public static ScenarioModuleExecution Execute(
        IReadOnlyList<EngineeringCalculationModuleValueDto> values,
        string sourceServiceName) =>
        new(
            EngineeringCalculationModuleExecutionStatus.Executed,
            values,
            [],
            [],
            [],
            sourceServiceName);

    public static ScenarioModuleExecution Skip(
        string message,
        string suggestedCorrection) =>
        new(
            EngineeringCalculationModuleExecutionStatus.Skipped,
            [],
            [
                new EngineeringWorkflowDiagnosticDto(
                    Severity: "warning",
                    Code: "SCENARIO_MODULE_SKIPPED",
                    Message: message,
                    SourceStep: "Review",
                    SuggestedCorrection: suggestedCorrection)
            ],
            [],
            [message],
            "EngineeringCalculationScenarioRunner");

    public static ScenarioModuleExecution Fail(
        string message,
        string suggestedCorrection) =>
        new(
            EngineeringCalculationModuleExecutionStatus.Failed,
            [],
            [
                new EngineeringWorkflowDiagnosticDto(
                    Severity: "error",
                    Code: "SCENARIO_MODULE_FAILED",
                    Message: message,
                    SourceStep: "Review",
                    SuggestedCorrection: suggestedCorrection)
            ],
            [],
            [],
            "EngineeringCalculationScenarioRunner");
}