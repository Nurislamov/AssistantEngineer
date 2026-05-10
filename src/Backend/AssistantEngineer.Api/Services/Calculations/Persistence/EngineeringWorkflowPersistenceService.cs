using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class EngineeringWorkflowPersistenceService : IEngineeringWorkflowPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly IEngineeringProjectRepository _projectRepository;
    private readonly IEngineeringWorkflowStateRepository _workflowStateRepository;
    private readonly IEngineeringCalculationScenarioRepository _scenarioRepository;
    private readonly IEngineeringCalculationArtifactRepository _artifactRepository;
    private readonly IEngineeringScenarioHistoryRepository _historyRepository;

    public EngineeringWorkflowPersistenceService(
        IEngineeringProjectRepository projectRepository,
        IEngineeringWorkflowStateRepository workflowStateRepository,
        IEngineeringCalculationScenarioRepository scenarioRepository,
        IEngineeringCalculationArtifactRepository artifactRepository,
        IEngineeringScenarioHistoryRepository historyRepository)
    {
        _projectRepository = projectRepository;
        _workflowStateRepository = workflowStateRepository;
        _scenarioRepository = scenarioRepository;
        _artifactRepository = artifactRepository;
        _historyRepository = historyRepository;
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

        var state = DeserializeWorkflowState(record.WorkflowStateJson);
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
        metadata["persistence"] = "in-memory-foundation";
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

        var normalizedDiagnostics = SortAndDistinctDiagnostics(validationDiagnostics ?? state.Diagnostics);
        var normalizedState = state with
        {
            Diagnostics = normalizedDiagnostics,
            Metadata = state.Metadata
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)
        };

        var versions = await _workflowStateRepository.ListVersionsByProjectIdAsync(state.ProjectId, cancellationToken);
        var nextVersion = versions.Count == 0 ? 1 : versions.Max(item => item.Version) + 1;
        var stateId = $"workflow-state-{state.ProjectId}-{nextVersion:D4}";

        var record = new EngineeringWorkflowStateRecordDto(
            WorkflowStateId: stateId,
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            Version: nextVersion,
            CurrentStep: state.CurrentStep,
            WorkflowStateJson: JsonSerializer.Serialize(normalizedState, JsonOptions),
            ValidationDiagnosticsJson: JsonSerializer.Serialize(normalizedDiagnostics, JsonOptions),
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

        var record = BuildScenarioRecord(
            scenarioRequest,
            scenarioResult,
            createdAtUtc: baseTimestamp,
            startedAtUtc: null,
            completedAtUtc: baseTimestamp);

        var persisted = await _scenarioRepository.CreateAsync(record, cancellationToken);

        await AppendHistoryAsync(
            scenarioResult.ScenarioId,
            persisted.ProjectId,
            EngineeringScenarioHistoryEventKind.Created,
            "Scenario record created from prepare-calculation request.",
            scenarioResult.ValidationDiagnostics,
            baseTimestamp,
            cancellationToken);

        await AppendHistoryAsync(
            scenarioResult.ScenarioId,
            persisted.ProjectId,
            EngineeringScenarioHistoryEventKind.Prepared,
            "Scenario request prepared without module execution.",
            scenarioResult.ValidationDiagnostics,
            baseTimestamp.AddMilliseconds(1),
            cancellationToken);

        await SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ValidationDiagnostics,
            "application/json",
            JsonSerializer.Serialize(SortAndDistinctDiagnostics(scenarioResult.ValidationDiagnostics), JsonOptions),
            baseTimestamp.AddMilliseconds(2),
            cancellationToken);

        await SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ScenarioResultJson,
            "application/json",
            JsonSerializer.Serialize(scenarioResult, JsonOptions),
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

        var updated = BuildScenarioRecord(
            scenarioRequest,
            scenarioResult,
            createdAtUtc: createdAt,
            startedAtUtc: startedAt,
            completedAtUtc: baseTimestamp);

        var persisted = existing is null
            ? await _scenarioRepository.CreateAsync(updated, cancellationToken)
            : await _scenarioRepository.UpdateAsync(updated, cancellationToken);

        var historyTime = baseTimestamp;
        if (existing is null)
        {
            await AppendHistoryAsync(
                scenarioResult.ScenarioId,
                persisted.ProjectId,
                EngineeringScenarioHistoryEventKind.Created,
                "Scenario record created from run-calculation request.",
                scenarioResult.ValidationDiagnostics,
                historyTime,
                cancellationToken);
            historyTime = historyTime.AddMilliseconds(1);
        }

        await AppendHistoryAsync(
            scenarioResult.ScenarioId,
            persisted.ProjectId,
            EngineeringScenarioHistoryEventKind.Started,
            "Scenario execution started.",
            scenarioResult.ValidationDiagnostics,
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
            scenarioResult.ValidationDiagnostics,
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
                scenarioResult.ValidationDiagnostics,
                historyTime,
                cancellationToken);
            historyTime = historyTime.AddMilliseconds(1);
        }

        await SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ValidationDiagnostics,
            "application/json",
            JsonSerializer.Serialize(SortAndDistinctDiagnostics(scenarioResult.ValidationDiagnostics), JsonOptions),
            historyTime,
            cancellationToken);
        historyTime = historyTime.AddMilliseconds(1);

        await SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ScenarioResultJson,
            "application/json",
            JsonSerializer.Serialize(scenarioResult, JsonOptions),
            historyTime,
            cancellationToken);
        historyTime = historyTime.AddMilliseconds(1);

        if (scenarioResult.CalculationTrace is not null)
        {
            await SaveScenarioArtifactAsync(
                scenarioResult.ScenarioId,
                EngineeringCalculationArtifactKind.TraceJson,
                "application/json",
                JsonSerializer.Serialize(scenarioResult.CalculationTrace, JsonOptions),
                historyTime,
                cancellationToken);
            historyTime = historyTime.AddMilliseconds(1);
        }

        if (!string.IsNullOrWhiteSpace(scenarioResult.ReportJson))
        {
            await SaveScenarioArtifactAsync(
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
            await SaveScenarioArtifactAsync(
                scenarioResult.ScenarioId,
                EngineeringCalculationArtifactKind.ReportJson,
                "application/json",
                JsonSerializer.Serialize(scenarioResult.EngineeringReport, JsonOptions),
                historyTime,
                cancellationToken);
            historyTime = historyTime.AddMilliseconds(1);
        }

        if (!string.IsNullOrWhiteSpace(scenarioResult.ReportMarkdown))
        {
            await SaveScenarioArtifactAsync(
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
            DiagnosticsJson: JsonSerializer.Serialize(SortAndDistinctDiagnostics(diagnostics), JsonOptions),
            CreatedAtUtc: timestampUtc);

        await _historyRepository.AppendAsync(entry, cancellationToken);
    }

    private async Task SaveScenarioArtifactAsync(
        string scenarioId,
        EngineeringCalculationArtifactKind artifactKind,
        string contentType,
        string content,
        DateTimeOffset timestampUtc,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var artifact = new EngineeringCalculationArtifactRecordDto(
            ArtifactId: $"{scenarioId}:{artifactKind}",
            ScenarioId: scenarioId,
            ArtifactKind: artifactKind,
            ContentType: contentType,
            Content: content,
            CreatedAtUtc: timestampUtc,
            SizeBytes: bytes.Length,
            ChecksumSha256: Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());

        await _artifactRepository.SaveAsync(artifact, cancellationToken);
    }

    private static EngineeringCalculationScenarioRecordDto BuildScenarioRecord(
        EngineeringCalculationScenarioRequestDto scenarioRequest,
        EngineeringCalculationScenarioResultDto scenarioResult,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        var requestJson = JsonSerializer.Serialize(scenarioRequest, JsonOptions);
        var summary = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["scenarioId"] = scenarioResult.ScenarioId,
            ["status"] = scenarioResult.Status.ToString(),
            ["executed"] = scenarioResult.Executed,
            ["executedModules"] = scenarioResult.ExecutedModules.Order(StringComparer.Ordinal).ToArray(),
            ["skippedModules"] = scenarioResult.SkippedModules.Order(StringComparer.Ordinal).ToArray(),
            ["unavailableModules"] = scenarioResult.UnavailableModules.Order(StringComparer.Ordinal).ToArray(),
            ["moduleSummaries"] = scenarioResult.ModuleSummaries,
            ["timings"] = scenarioResult.Timings.OrderBy(item => item.ModuleKind, StringComparer.Ordinal).ToArray()
        };

        var diagnostics = SortAndDistinctDiagnostics(scenarioResult.ValidationDiagnostics);
        var resultSummaryJson = JsonSerializer.Serialize(summary, JsonOptions);
        var diagnosticsJson = JsonSerializer.Serialize(diagnostics, JsonOptions);
        var durationMilliseconds = scenarioResult.Timings.Sum(item => item.DurationMilliseconds);
        var projectId = scenarioRequest.ProjectId ?? scenarioRequest.State.ProjectId;

        return new EngineeringCalculationScenarioRecordDto(
            ScenarioId: scenarioResult.ScenarioId,
            ProjectId: projectId,
            BuildingId: scenarioRequest.BuildingId ?? scenarioRequest.State.BuildingId,
            ScenarioKind: scenarioRequest.ScenarioKind,
            ExecutionMode: scenarioRequest.ExecutionMode,
            Status: scenarioResult.Status,
            RequestJson: requestJson,
            ResultSummaryJson: resultSummaryJson,
            CreatedAtUtc: createdAtUtc,
            StartedAtUtc: startedAtUtc,
            CompletedAtUtc: completedAtUtc,
            DurationMilliseconds: durationMilliseconds > 0.0 ? durationMilliseconds : null,
            DiagnosticsJson: diagnosticsJson);
    }

    private static EngineeringWorkflowStateDto? DeserializeWorkflowState(string rawJson)
    {
        try
        {
            return JsonSerializer.Deserialize<EngineeringWorkflowStateDto>(rawJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return diagnostics
            .OrderByDescending(item => SeverityRank(item.Severity))
            .ThenBy(item => item.SourceStep, StringComparer.Ordinal)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .Where(item => seen.Add($"{item.SourceStep}|{item.Code}|{item.Message}|{item.TargetField}"))
            .ToArray();
    }

    private static int SeverityRank(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 1;
    }
}
