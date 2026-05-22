using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationScenarioResultBuilder
{
    CalculationTraceDocument BuildTrace(
        EngineeringCalculationScenarioRequestDto request,
        IReadOnlyList<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> warnings);

    EngineeringCalculationScenarioResultDto BuildScenarioResult(
        EngineeringCalculationScenarioRequestDto request,
        IReadOnlyList<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        IReadOnlyList<EngineeringCalculationModuleTimingDto> timings,
        IReadOnlyList<string> executedModules,
        IReadOnlyList<string> skippedModules,
        IReadOnlyList<string> unavailableModules,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> warnings,
        string? topologySummary,
        string? ventilationSummary,
        string? groundSummary,
        string? heatingCoolingSummaryText,
        string? dhwSummary,
        string? systemEnergySummaryText,
        BuildingEnergyBalanceResult? heatingCoolingResult,
        CalculationTraceDocument? calculationTrace,
        bool includeReport,
        IReadOnlyList<string>? reportFormats);

    string FindModuleSummary(
        IEnumerable<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        string moduleKind);
}