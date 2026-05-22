using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationScenarioRequestValidator
{
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Validate(EngineeringCalculationScenarioRequestDto request);

    IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinct(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics);

    bool HasErrors(IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics);
}