using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;
using ApiWorkflowPersistence = AssistantEngineer.Api.Services.Calculations.Persistence.IEngineeringWorkflowPersistenceService;

namespace AssistantEngineer.Api.Services.Calculations.Composition;

internal sealed class EngineeringWorkflowScenarioRunnerAdapter : IEngineeringWorkflowScenarioRunner
{
    private readonly IEngineeringCalculationScenarioRunner _inner;

    public EngineeringWorkflowScenarioRunnerAdapter(IEngineeringCalculationScenarioRunner inner)
    {
        _inner = inner;
    }

    public Task<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioResultDto> RunAsync(
        AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioRequestDto request,
        CancellationToken cancellationToken)
    {
        return _inner.RunAsync(request, cancellationToken);
    }
}

internal sealed class EngineeringWorkflowJobServiceAdapter : IEngineeringWorkflowJobService
{
    private readonly IEngineeringCalculationJobService _inner;

    public EngineeringWorkflowJobServiceAdapter(IEngineeringCalculationJobService inner)
    {
        _inner = inner;
    }

    public Task<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationJobResultDto> CreateOrRunJobAsync(
        AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationJobRequestDto request,
        CancellationToken cancellationToken)
    {
        return _inner.CreateOrRunJobAsync(request, cancellationToken);
    }

    public Task<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationJobResultDto?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        return _inner.GetJobAsync(jobId, cancellationToken);
    }
}

internal sealed class EngineeringWorkflowPersistenceServiceAdapter : AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.IEngineeringWorkflowPersistenceService
{
    private readonly ApiWorkflowPersistence _inner;

    public EngineeringWorkflowPersistenceServiceAdapter(ApiWorkflowPersistence inner)
    {
        _inner = inner;
    }

    public AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProviderInfo GetProviderInfo()
    {
        var inner = _inner.GetProviderInfo();
        var provider = inner.Provider switch
        {
            AssistantEngineer.Api.Services.Calculations.Persistence.EngineeringWorkflowPersistenceProvider.SQLite => AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProvider.SQLite,
            AssistantEngineer.Api.Services.Calculations.Persistence.EngineeringWorkflowPersistenceProvider.None => AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProvider.None,
            _ => AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProvider.InMemory
        };

        return new AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProviderInfo(
            provider,
            inner.DurableEnabled,
            inner.ProviderLabel);
    }

    public Task<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringWorkflowStateDto?> GetLatestWorkflowStateAsync(int projectId, int? buildingId, CancellationToken cancellationToken)
    {
        return _inner.GetLatestWorkflowStateAsync(projectId, buildingId, cancellationToken);
    }

    public Task<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringWorkflowStateRecordDto> SaveWorkflowStateAsync(AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringWorkflowStateDto state, IReadOnlyList<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringWorkflowDiagnosticDto>? validationDiagnostics, CancellationToken cancellationToken)
    {
        return _inner.SaveWorkflowStateAsync(state, validationDiagnostics, cancellationToken);
    }

    public Task<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioRecordDto> SavePreparedScenarioAsync(AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioRequestDto scenarioRequest, AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioResultDto scenarioResult, CancellationToken cancellationToken)
    {
        return _inner.SavePreparedScenarioAsync(scenarioRequest, scenarioResult, cancellationToken);
    }

    public Task<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioRecordDto> SaveRunScenarioAsync(AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioRequestDto scenarioRequest, AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioResultDto scenarioResult, CancellationToken cancellationToken)
    {
        return _inner.SaveRunScenarioAsync(scenarioRequest, scenarioResult, cancellationToken);
    }

    public Task<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioRecordDto?> GetScenarioAsync(string scenarioId, CancellationToken cancellationToken)
    {
        return _inner.GetScenarioAsync(scenarioId, cancellationToken);
    }

    public Task<IReadOnlyList<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationScenarioRecordDto>> ListProjectScenariosAsync(int projectId, CancellationToken cancellationToken)
    {
        return _inner.ListProjectScenariosAsync(projectId, cancellationToken);
    }

    public Task<IReadOnlyList<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationArtifactRecordDto>> ListScenarioArtifactsAsync(string scenarioId, CancellationToken cancellationToken)
    {
        return _inner.ListScenarioArtifactsAsync(scenarioId, cancellationToken);
    }

    public Task<AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationArtifactRecordDto?> GetScenarioArtifactAsync(string scenarioId, AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow.EngineeringCalculationArtifactKind artifactKind, CancellationToken cancellationToken)
    {
        return _inner.GetScenarioArtifactAsync(scenarioId, artifactKind, cancellationToken);
    }
}
