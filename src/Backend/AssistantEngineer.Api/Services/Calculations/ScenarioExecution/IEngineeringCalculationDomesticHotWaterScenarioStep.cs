using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationDomesticHotWaterScenarioStep
{
    EngineeringCalculationDomesticHotWaterScenarioStepResult Execute(EngineeringCalculationScenarioRequestDto request);
}

public sealed record EngineeringCalculationDomesticHotWaterScenarioStepResult(
    ScenarioModuleExecution Execution,
    DomesticHotWaterSystemLoadFoundationResult? Summary,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics);