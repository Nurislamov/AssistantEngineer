using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public interface IEngineeringWorkflowDiagnosticsService
{
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> ValidateState(EngineeringWorkflowStateDto state);

    IReadOnlyList<EngineeringWorkflowStepDto> BuildStepStatuses(
        EngineeringWorkflowStateDto state,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics);

    IReadOnlyList<EngineeringWorkflowStepDto> BuildStepStatusesForMissingBuilding(
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics);

    EngineeringWorkflowStateDto AddMissingPersistedStateDiagnostic(
        EngineeringWorkflowStateDto state,
        EngineeringWorkflowPersistenceProviderInfo providerInfo);

    IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics);

    string SelectCurrentStep(
        IReadOnlyList<EngineeringWorkflowZoneDto> zones,
        IReadOnlyList<EngineeringWorkflowBoundaryDto> boundaries,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics);

    string MapBoundaryIndicator(WallBoundaryTypeDto boundaryType);

    string NormalizeSeverity(string? source);

    string NormalizeSeverity(BuildingCalculationReadinessSeverity severity);

    bool IsErrorSeverity(string severity);
}
