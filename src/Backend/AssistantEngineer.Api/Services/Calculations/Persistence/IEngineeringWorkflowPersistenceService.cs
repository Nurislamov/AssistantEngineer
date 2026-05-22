using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public interface IEngineeringWorkflowPersistenceService
{
    EngineeringWorkflowPersistenceProviderInfo GetProviderInfo();

    Task<EngineeringWorkflowStateDto?> GetLatestWorkflowStateAsync(
        int projectId,
        int? buildingId,
        CancellationToken cancellationToken);

    Task<EngineeringWorkflowStateRecordDto> SaveWorkflowStateAsync(
        EngineeringWorkflowStateDto state,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto>? validationDiagnostics,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationScenarioRecordDto> SavePreparedScenarioAsync(
        EngineeringCalculationScenarioRequestDto scenarioRequest,
        EngineeringCalculationScenarioResultDto scenarioResult,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationScenarioRecordDto> SaveRunScenarioAsync(
        EngineeringCalculationScenarioRequestDto scenarioRequest,
        EngineeringCalculationScenarioResultDto scenarioResult,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationScenarioRecordDto?> GetScenarioAsync(
        string scenarioId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListProjectScenariosAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListScenarioArtifactsAsync(
        string scenarioId,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationArtifactRecordDto?> GetScenarioArtifactAsync(
        string scenarioId,
        EngineeringCalculationArtifactKind artifactKind,
        CancellationToken cancellationToken);
}
