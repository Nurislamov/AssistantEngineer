using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationSystemEnergyScenarioStep
{
    EngineeringCalculationSystemEnergyScenarioStepResult Execute(
        EngineeringCalculationScenarioRequestDto request,
        DomesticHotWaterSystemLoadFoundationResult? domesticHotWaterSummary);
}

public sealed record EngineeringCalculationSystemEnergyScenarioStepResult(
    ScenarioModuleExecution Execution,
    SystemEnergyCalculationResult? Summary,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics);