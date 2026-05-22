using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class EngineeringWorkflowPersistenceService : IEngineeringWorkflowPersistenceService
{
    private readonly IEngineeringProjectRepository _projectRepository;
    private readonly IEngineeringWorkflowStateRepository _workflowStateRepository;
    private readonly IEngineeringCalculationScenarioRepository _scenarioRepository;
    private readonly IEngineeringCalculationArtifactRepository _artifactRepository;
    private readonly IEngineeringScenarioHistoryRepository _historyRepository;
    private readonly EngineeringWorkflowPersistenceOptions _options;
    private readonly EngineeringWorkflowPayloadLimitsOptions _payloadLimits;
    private readonly EngineeringWorkflowPersistencePayloadService _payloadService;
    private readonly EngineeringWorkflowArtifactPersistenceService _artifactPersistenceService;

    public EngineeringWorkflowPersistenceService(
        IEngineeringProjectRepository projectRepository,
        IEngineeringWorkflowStateRepository workflowStateRepository,
        IEngineeringCalculationScenarioRepository scenarioRepository,
        IEngineeringCalculationArtifactRepository artifactRepository,
        IEngineeringScenarioHistoryRepository historyRepository,
        IOptions<EngineeringWorkflowPersistenceOptions> options)
    {
        _projectRepository = projectRepository;
        _workflowStateRepository = workflowStateRepository;
        _scenarioRepository = scenarioRepository;
        _artifactRepository = artifactRepository;
        _historyRepository = historyRepository;
        _options = options.Value;
        _payloadLimits = _options.PayloadLimits;
        _payloadService = new EngineeringWorkflowPersistencePayloadService(_payloadLimits);
        _artifactPersistenceService = new EngineeringWorkflowArtifactPersistenceService(
            _artifactRepository,
            _payloadService,
            _payloadLimits);
    }

    public EngineeringWorkflowPersistenceProviderInfo GetProviderInfo()
    {
        var provider = _options.Provider switch
        {
            EngineeringWorkflowPersistenceProvider.SQLite => EngineeringWorkflowPersistenceProvider.SQLite,
            EngineeringWorkflowPersistenceProvider.None => EngineeringWorkflowPersistenceProvider.InMemory,
            _ => EngineeringWorkflowPersistenceProvider.InMemory
        };

        return provider switch
        {
            EngineeringWorkflowPersistenceProvider.SQLite => new EngineeringWorkflowPersistenceProviderInfo(
                Provider: provider,
                DurableEnabled: true,
                ProviderLabel: "sqlite-foundation"),
            _ => new EngineeringWorkflowPersistenceProviderInfo(
                Provider: EngineeringWorkflowPersistenceProvider.InMemory,
                DurableEnabled: false,
                ProviderLabel: "in-memory-foundation")
        };
    }

    public async Task<EngineeringWorkflowStateDto?> GetLatestWorkflowStateAsync(
        int projectId,
        int? buildingId,
        CancellationToken cancellationToken)
    {
        var record = await _workflowStateRepository.GetLatestByProjectIdAsync(projectId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var state = _payloadService.DeserializeWorkflowState(record.WorkflowStateJson);
        if (state is null)
        {
            return null;
        }

        if (buildingId.HasValue && state.BuildingId != buildingId.Value)
        {
            return null;
        }

        var metadata = state.Metadata
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        var providerInfo = GetProviderInfo();
        metadata["persistence"] = providerInfo.ProviderLabel;
        metadata["persistenceProvider"] = providerInfo.Provider.ToString();
        metadata["durablePersistenceEnabled"] = providerInfo.DurableEnabled ? "true" : "false";
        metadata["workflowStateId"] = record.WorkflowStateId;
        metadata["workflowStateVersion"] = record.Version.ToString();

        return state with { Metadata = metadata };
    }

    public async Task<EngineeringWorkflowStateRecordDto> SaveWorkflowStateAsync(
        EngineeringWorkflowStateDto state,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto>? validationDiagnostics,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await EnsureProjectRecordAsync(state.ProjectId, state.ProjectName, state.Metadata, now, cancellationToken);

        var normalizedDiagnostics = _payloadService.SortAndDistinctDiagnostics(validationDiagnostics ?? state.Diagnostics).ToList();
        var normalizedState = state with
        {
            Diagnostics = normalizedDiagnostics,
            Metadata = state.Metadata
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)
        };

        var statePayload = _payloadService.SerializeStatePayload(normalizedState, normalizedDiagnostics);
        var diagnosticsJsonPayload = _payloadService.ApplyPayloadLimit(
            "workflow-state-validation-diagnostics",
            _payloadService.Serialize(statePayload.Diagnostics),
            _payloadLimits.DiagnosticsJsonMaxBytes,
            contentType: "application/json");

        var versions = await _workflowStateRepository.ListVersionsByProjectIdAsync(state.ProjectId, cancellationToken);
        var nextVersion = versions.Count == 0 ? 1 : versions.Max(item => item.Version) + 1;
        var stateId = $"workflow-state-{state.ProjectId}-{nextVersion:D4}";

        var record = new EngineeringWorkflowStateRecordDto(
            WorkflowStateId: stateId,
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            Version: nextVersion,
            CurrentStep: state.CurrentStep,
            WorkflowStateJson: statePayload.Content,
            ValidationDiagnosticsJson: diagnosticsJsonPayload.Content,
            CreatedAtUtc: now,
            UpdatedAtUtc: now);

        return await _workflowStateRepository.SaveAsync(record, cancellationToken);
    }

    public async Task<EngineeringCalculationScenarioRecordDto> SavePreparedScenarioAsync(
        EngineeringCalculationScenarioRequestDto scenarioRequest,
        EngineeringCalculationScenarioResultDto scenarioResult,
        CancellationToken cancellationToken)
    {
        var baseTimestamp = scenarioRequest.DeterministicTimestampUtc ?? DateTimeOffset.UtcNow;
        await EnsureProjectRecordAsync(
            scenarioRequest.ProjectId ?? scenarioRequest.State.ProjectId,
            scenarioRequest.State.ProjectName,
            scenarioRequest.State.Metadata,
            baseTimestamp,
            cancellationToken);

        var scenarioDiagnostics = _payloadService.SortAndDistinctDiagnostics(scenarioResult.ValidationDiagnostics).ToList();
        var recordBuildResult = _payloadService.BuildScenarioRecord(
            scenarioRequest,
            scenarioResult,
            scenarioDiagnostics,
            createdAtUtc: baseTimestamp,
            startedAtUtc: null,
            completedAtUtc: baseTimestamp);
        var persistedScenarioResult = scenarioResult with
        {
            ValidationDiagnostics = recordBuildResult.Diagnostics
        };

        var persisted = await _scenarioRepository.CreateAsync(recordBuildResult.Record, cancellationToken);

        await AppendHistoryAsync(
            scenarioResult.ScenarioId,
            persisted.ProjectId,
            EngineeringScenarioHistoryEventKind.Created,
            "Scenario record created from prepare-calculation request.",
            recordBuildResult.Diagnostics,
            baseTimestamp,
            cancellationToken);

        await AppendHistoryAsync(
            scenarioResult.ScenarioId,
            persisted.ProjectId,
            EngineeringScenarioHistoryEventKind.Prepared,
            "Scenario request prepared without module execution.",
            recordBuildResult.Diagnostics,
            baseTimestamp.AddMilliseconds(1),
            cancellationToken);

        await _artifactPersistenceService.SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ValidationDiagnostics,
            "application/json",
            _payloadService.Serialize(recordBuildResult.Diagnostics),
            baseTimestamp.AddMilliseconds(2),
            cancellationToken);

        await _artifactPersistenceService.SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ScenarioResultJson,
            "application/json",
            _payloadService.Serialize(persistedScenarioResult),
            baseTimestamp.AddMilliseconds(3),
            cancellationToken);

        return persisted;
    }

    public async Task<EngineeringCalculationScenarioRecordDto> SaveRunScenarioAsync(
        EngineeringCalculationScenarioRequestDto scenarioRequest,
        EngineeringCalculationScenarioResultDto scenarioResult,
        CancellationToken cancellationToken)
    {
        var baseTimestamp = scenarioRequest.DeterministicTimestampUtc ?? DateTimeOffset.UtcNow;
        await EnsureProjectRecordAsync(
            scenarioRequest.ProjectId ?? scenarioRequest.State.ProjectId,
            scenarioRequest.State.ProjectName,
            scenarioRequest.State.Metadata,
            baseTimestamp,
            cancellationToken);

        var existing = await _scenarioRepository.GetByIdAsync(scenarioResult.ScenarioId, cancellationToken);
        var createdAt = existing?.CreatedAtUtc ?? baseTimestamp;
        var startedAt = existing?.StartedAtUtc ?? baseTimestamp;

        var scenarioDiagnostics = _payloadService.SortAndDistinctDiagnostics(scenarioResult.ValidationDiagnostics).ToList();
        var recordBuildResult = _payloadService.BuildScenarioRecord(
            scenarioRequest,
            scenarioResult,
            scenarioDiagnostics,
            createdAtUtc: createdAt,
            startedAtUtc: startedAt,
            completedAtUtc: baseTimestamp);
        var persistedScenarioResult = scenarioResult with
        {
            ValidationDiagnostics = recordBuildResult.Diagnostics
        };

        var persisted = existing is null
            ? await _scenarioRepository.CreateAsync(recordBuildResult.Record, cancellationToken)
            : await _scenarioRepository.UpdateAsync(recordBuildResult.Record, cancellationToken);

        var historyTime = baseTimestamp;
        if (existing is null)
        {
            await AppendHistoryAsync(
                scenarioResult.ScenarioId,
                persisted.ProjectId,
                EngineeringScenarioHistoryEventKind.Created,
                "Scenario record created from run-calculation request.",
                recordBuildResult.Diagnostics,
                historyTime,
                cancellationToken);
            historyTime = historyTime.AddMilliseconds(1);
        }

        await AppendHistoryAsync(
            scenarioResult.ScenarioId,
            persisted.ProjectId,
            EngineeringScenarioHistoryEventKind.Started,
            "Scenario execution started.",
            recordBuildResult.Diagnostics,
            historyTime,
            cancellationToken);
        historyTime = historyTime.AddMilliseconds(1);

        var completionEventKind = scenarioResult.Status is EngineeringCalculationExecutionStatus.FailedExecution or EngineeringCalculationExecutionStatus.FailedValidation
            ? EngineeringScenarioHistoryEventKind.Failed
            : EngineeringScenarioHistoryEventKind.Completed;

        await AppendHistoryAsync(
            scenarioResult.ScenarioId,
            persisted.ProjectId,
            completionEventKind,
            $"Scenario execution completed with status `{scenarioResult.Status}`.",
            recordBuildResult.Diagnostics,
            historyTime,
            cancellationToken);
        historyTime = historyTime.AddMilliseconds(1);

        if (scenarioResult.EngineeringReport is not null ||
            !string.IsNullOrWhiteSpace(scenarioResult.ReportJson) ||
            !string.IsNullOrWhiteSpace(scenarioResult.ReportMarkdown))
        {
            await AppendHistoryAsync(
                scenarioResult.ScenarioId,
                persisted.ProjectId,
                EngineeringScenarioHistoryEventKind.ReportGenerated,
                "Engineering report artifact was generated by scenario execution.",
                recordBuildResult.Diagnostics,
                historyTime,
                cancellationToken);
            historyTime = historyTime.AddMilliseconds(1);
        }

        await _artifactPersistenceService.SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ValidationDiagnostics,
            "application/json",
            _payloadService.Serialize(recordBuildResult.Diagnostics),
            historyTime,
            cancellationToken);
        historyTime = historyTime.AddMilliseconds(1);

        await _artifactPersistenceService.SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ScenarioResultJson,
            "application/json",
            _payloadService.Serialize(persistedScenarioResult),
            historyTime,
            cancellationToken);
        historyTime = historyTime.AddMilliseconds(1);

        if (scenarioResult.CalculationTrace is not null)
        {
            await _artifactPersistenceService.SaveScenarioArtifactAsync(
                scenarioResult.ScenarioId,
                EngineeringCalculationArtifactKind.TraceJson,
                "application/json",
                _payloadService.Serialize(scenarioResult.CalculationTrace),
                historyTime,
                cancellationToken);
            historyTime = historyTime.AddMilliseconds(1);
        }

        if (!string.IsNullOrWhiteSpace(scenarioResult.ReportJson))
        {
            await _artifactPersistenceService.SaveScenarioArtifactAsync(
                scenarioResult.ScenarioId,
                EngineeringCalculationArtifactKind.ReportJson,
                "application/json",
                scenarioResult.ReportJson,
                historyTime,
                cancellationToken);
            historyTime = historyTime.AddMilliseconds(1);
        }
        else if (scenarioResult.EngineeringReport is not null)
        {
            await _artifactPersistenceService.SaveScenarioArtifactAsync(
                scenarioResult.ScenarioId,
                EngineeringCalculationArtifactKind.ReportJson,
                "application/json",
                _payloadService.Serialize(scenarioResult.EngineeringReport),
                historyTime,
                cancellationToken);
            historyTime = historyTime.AddMilliseconds(1);
        }

        if (!string.IsNullOrWhiteSpace(scenarioResult.ReportMarkdown))
        {
            await _artifactPersistenceService.SaveScenarioArtifactAsync(
                scenarioResult.ScenarioId,
                EngineeringCalculationArtifactKind.ReportMarkdown,
                "text/markdown",
                scenarioResult.ReportMarkdown,
                historyTime,
                cancellationToken);
        }

        return persisted;
    }

    public Task<EngineeringCalculationScenarioRecordDto?> GetScenarioAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        return _scenarioRepository.GetByIdAsync(scenarioId, cancellationToken);
    }

    public Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListProjectScenariosAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        return _scenarioRepository.ListByProjectIdAsync(projectId, cancellationToken);
    }

    public Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListScenarioArtifactsAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        return _artifactRepository.ListByScenarioIdAsync(scenarioId, cancellationToken);
    }

    public Task<EngineeringCalculationArtifactRecordDto?> GetScenarioArtifactAsync(
        string scenarioId,
        EngineeringCalculationArtifactKind artifactKind,
        CancellationToken cancellationToken)
    {
        return _artifactRepository.GetByScenarioAndKindAsync(scenarioId, artifactKind, cancellationToken);
    }

    private async Task EnsureProjectRecordAsync(
        int projectId,
        string projectName,
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset timestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var record = new EngineeringProjectRecordDto(
            ProjectId: projectId,
            ProjectName: projectName,
            Description: "Internal engineering workflow project persistence record.",
            CreatedAtUtc: timestampUtc,
            UpdatedAtUtc: timestampUtc,
            Status: EngineeringProjectRecordStatus.Active,
            MetadataJson: metadata
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));

        await _projectRepository.UpsertAsync(record, cancellationToken);
    }

    private async Task AppendHistoryAsync(
        string scenarioId,
        int projectId,
        EngineeringScenarioHistoryEventKind eventKind,
        string message,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        DateTimeOffset timestampUtc,
        CancellationToken cancellationToken)
    {
        var eventId = $"{scenarioId}:{eventKind}:{timestampUtc.ToUnixTimeMilliseconds()}";
        var entry = new EngineeringScenarioHistoryEntryDto(
            EventId: eventId,
            ScenarioId: scenarioId,
            ProjectId: projectId,
            EventKind: eventKind,
            Message: message,
            DiagnosticsJson: _payloadService.Serialize(_payloadService.SortAndDistinctDiagnostics(diagnostics)),
            CreatedAtUtc: timestampUtc);

        await _historyRepository.AppendAsync(entry, cancellationToken);
    }
}
