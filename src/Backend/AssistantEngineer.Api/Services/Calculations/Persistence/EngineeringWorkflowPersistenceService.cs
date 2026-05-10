using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Api.Contracts.Calculations;
using Microsoft.Extensions.Options;

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
    private readonly EngineeringWorkflowPersistenceOptions _options;
    private readonly EngineeringWorkflowPayloadLimitsOptions _payloadLimits;

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

        var normalizedDiagnostics = SortAndDistinctDiagnostics(validationDiagnostics ?? state.Diagnostics).ToList();
        var normalizedState = state with
        {
            Diagnostics = normalizedDiagnostics,
            Metadata = state.Metadata
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)
        };

        var statePayload = SerializeStatePayload(normalizedState, normalizedDiagnostics);
        var diagnosticsJsonPayload = ApplyPayloadLimit(
            "workflow-state-validation-diagnostics",
            JsonSerializer.Serialize(statePayload.Diagnostics, JsonOptions),
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

        var scenarioDiagnostics = SortAndDistinctDiagnostics(scenarioResult.ValidationDiagnostics).ToList();
        var recordBuildResult = BuildScenarioRecord(
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

        await SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ValidationDiagnostics,
            "application/json",
            JsonSerializer.Serialize(recordBuildResult.Diagnostics, JsonOptions),
            baseTimestamp.AddMilliseconds(2),
            cancellationToken);

        await SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ScenarioResultJson,
            "application/json",
            JsonSerializer.Serialize(persistedScenarioResult, JsonOptions),
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

        var scenarioDiagnostics = SortAndDistinctDiagnostics(scenarioResult.ValidationDiagnostics).ToList();
        var recordBuildResult = BuildScenarioRecord(
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

        await SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ValidationDiagnostics,
            "application/json",
            JsonSerializer.Serialize(recordBuildResult.Diagnostics, JsonOptions),
            historyTime,
            cancellationToken);
        historyTime = historyTime.AddMilliseconds(1);

        await SaveScenarioArtifactAsync(
            scenarioResult.ScenarioId,
            EngineeringCalculationArtifactKind.ScenarioResultJson,
            "application/json",
            JsonSerializer.Serialize(persistedScenarioResult, JsonOptions),
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
        var limitedContent = ApplyPayloadLimit(
            $"scenario-artifact-{artifactKind}",
            content,
            _payloadLimits.ArtifactContentMaxBytes,
            contentType);
        var bytes = Encoding.UTF8.GetBytes(limitedContent.Content);
        var artifact = new EngineeringCalculationArtifactRecordDto(
            ArtifactId: $"{scenarioId}:{artifactKind}",
            ScenarioId: scenarioId,
            ArtifactKind: artifactKind,
            ContentType: contentType,
            Content: limitedContent.Content,
            CreatedAtUtc: timestampUtc,
            SizeBytes: bytes.Length,
            ChecksumSha256: Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());

        await _artifactRepository.SaveAsync(artifact, cancellationToken);
    }

    private ScenarioRecordBuildResult BuildScenarioRecord(
        EngineeringCalculationScenarioRequestDto scenarioRequest,
        EngineeringCalculationScenarioResultDto scenarioResult,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        var mutableDiagnostics = diagnostics.ToList();
        var requestJsonPayload = ApplyPayloadLimit(
            "scenario-request-json",
            JsonSerializer.Serialize(scenarioRequest, JsonOptions),
            _payloadLimits.RequestJsonMaxBytes,
            contentType: "application/json");
        if (requestJsonPayload.WasTruncated)
        {
            mutableDiagnostics.Add(CreatePayloadDiagnostic(
                code: "WORKFLOW_REQUEST_JSON_TRUNCATED",
                message: $"Scenario request payload exceeded `{_payloadLimits.RequestJsonMaxBytes}` bytes and was truncated for persistence.",
                sourceStep: "Review",
                targetField: "requestJson"));
        }

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

        var resultSummaryPayload = ApplyPayloadLimit(
            "scenario-result-summary-json",
            JsonSerializer.Serialize(summary, JsonOptions),
            _payloadLimits.ResultSummaryJsonMaxBytes,
            contentType: "application/json");
        if (resultSummaryPayload.WasTruncated)
        {
            mutableDiagnostics.Add(CreatePayloadDiagnostic(
                code: "WORKFLOW_RESULT_SUMMARY_JSON_TRUNCATED",
                message: $"Scenario result summary payload exceeded `{_payloadLimits.ResultSummaryJsonMaxBytes}` bytes and was truncated for persistence.",
                sourceStep: "Review",
                targetField: "resultSummaryJson"));
        }

        var normalizedDiagnostics = SortAndDistinctDiagnostics(mutableDiagnostics);
        var diagnosticsJsonPayload = ApplyPayloadLimit(
            "scenario-diagnostics-json",
            JsonSerializer.Serialize(normalizedDiagnostics, JsonOptions),
            _payloadLimits.DiagnosticsJsonMaxBytes,
            contentType: "application/json");
        if (diagnosticsJsonPayload.WasTruncated)
        {
            normalizedDiagnostics = SortAndDistinctDiagnostics(normalizedDiagnostics.Append(CreatePayloadDiagnostic(
                code: "WORKFLOW_DIAGNOSTICS_JSON_TRUNCATED",
                message: $"Scenario diagnostics payload exceeded `{_payloadLimits.DiagnosticsJsonMaxBytes}` bytes and was truncated for persistence.",
                sourceStep: "Validation",
                targetField: "diagnosticsJson")));
            diagnosticsJsonPayload = ApplyPayloadLimit(
                "scenario-diagnostics-json",
                JsonSerializer.Serialize(normalizedDiagnostics, JsonOptions),
                _payloadLimits.DiagnosticsJsonMaxBytes,
                contentType: "application/json");
        }

        var durationMilliseconds = scenarioResult.Timings.Sum(item => item.DurationMilliseconds);
        var projectId = scenarioRequest.ProjectId ?? scenarioRequest.State.ProjectId;

        return new ScenarioRecordBuildResult(
            Record: new EngineeringCalculationScenarioRecordDto(
            ScenarioId: scenarioResult.ScenarioId,
            ProjectId: projectId,
            BuildingId: scenarioRequest.BuildingId ?? scenarioRequest.State.BuildingId,
            ScenarioKind: scenarioRequest.ScenarioKind,
            ExecutionMode: scenarioRequest.ExecutionMode,
            Status: scenarioResult.Status,
            RequestJson: requestJsonPayload.Content,
            ResultSummaryJson: resultSummaryPayload.Content,
            CreatedAtUtc: createdAtUtc,
            StartedAtUtc: startedAtUtc,
            CompletedAtUtc: completedAtUtc,
            DurationMilliseconds: durationMilliseconds > 0.0 ? durationMilliseconds : null,
            DiagnosticsJson: diagnosticsJsonPayload.Content),
            Diagnostics: normalizedDiagnostics);
    }

    private PayloadContent SerializeStatePayload(
        EngineeringWorkflowStateDto state,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> normalizedDiagnostics)
    {
        var serializedState = JsonSerializer.Serialize(state, JsonOptions);
        if (!_payloadLimits.Enabled || Utf8ByteCount(serializedState) <= _payloadLimits.StateJsonMaxBytes)
        {
            return new PayloadContent(serializedState, normalizedDiagnostics, WasTruncated: false, OriginalBytes: Utf8ByteCount(serializedState), StoredBytes: Utf8ByteCount(serializedState));
        }

        var truncationDiagnostic = CreatePayloadDiagnostic(
            code: "WORKFLOW_STATE_JSON_TRUNCATED",
            message: $"Workflow state payload exceeded `{_payloadLimits.StateJsonMaxBytes}` bytes and was compacted for persistence.",
            sourceStep: "Validation",
            targetField: "workflowStateJson");
        var compactDiagnostics = SortAndDistinctDiagnostics(normalizedDiagnostics.Append(truncationDiagnostic));
        var compactState = state with
        {
            Zones = [],
            Boundaries = [],
            Diagnostics = compactDiagnostics,
            Assumptions = SortAndDistinctAssumptions(
                state.Assumptions.Append("Workflow state snapshot was compacted by persistence payload limits.")),
            Links = [],
            CalculationTraceSummary = null,
            ReportSummary = null,
            VentilationSettings = state.VentilationSettings with { Warnings = [] },
            Metadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["payloadStateJsonTruncated"] = "true",
                ["payloadTruncationMarker"] = _payloadLimits.TruncationMarker
            }
        };

        var compactSerializedState = JsonSerializer.Serialize(compactState, JsonOptions);
        if (Utf8ByteCount(compactSerializedState) > _payloadLimits.StateJsonMaxBytes)
        {
            var hardLimitedPayload = ApplyPayloadLimit(
                "workflow-state-json-fallback",
                compactSerializedState,
                _payloadLimits.StateJsonMaxBytes,
                contentType: "application/json");
            return new PayloadContent(
                hardLimitedPayload.Content,
                compactDiagnostics,
                WasTruncated: true,
                OriginalBytes: Utf8ByteCount(serializedState),
                StoredBytes: hardLimitedPayload.StoredBytes);
        }

        return new PayloadContent(
            compactSerializedState,
            compactDiagnostics,
            WasTruncated: true,
            OriginalBytes: Utf8ByteCount(serializedState),
            StoredBytes: Utf8ByteCount(compactSerializedState));
    }

    private PayloadContent ApplyPayloadLimit(
        string payloadName,
        string content,
        int maxBytes,
        string contentType)
    {
        var originalBytes = Utf8ByteCount(content);
        if (!_payloadLimits.Enabled || originalBytes <= maxBytes)
        {
            return new PayloadContent(content, [], WasTruncated: false, OriginalBytes: originalBytes, StoredBytes: originalBytes);
        }

        if (string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            var previewBudget = Math.Max(16, maxBytes / 3);
            string envelopeJson;
            do
            {
                var envelope = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["truncated"] = true,
                    ["payloadName"] = payloadName,
                    ["marker"] = _payloadLimits.TruncationMarker,
                    ["maxBytes"] = maxBytes,
                    ["originalBytes"] = originalBytes,
                    ["preview"] = TruncateUtf8WithMarker(content, previewBudget)
                };
                envelopeJson = JsonSerializer.Serialize(envelope, JsonOptions);
                previewBudget = Math.Max(8, previewBudget / 2);
            }
            while (Utf8ByteCount(envelopeJson) > maxBytes && previewBudget > 8);

            if (Utf8ByteCount(envelopeJson) > maxBytes)
            {
                envelopeJson = TruncateUtf8WithMarker(envelopeJson, maxBytes);
            }

            return new PayloadContent(
                envelopeJson,
                [],
                WasTruncated: true,
                OriginalBytes: originalBytes,
                StoredBytes: Utf8ByteCount(envelopeJson));
        }

        var truncated = TruncateUtf8WithMarker(content, maxBytes);
        return new PayloadContent(
            truncated,
            [],
            WasTruncated: true,
            OriginalBytes: originalBytes,
            StoredBytes: Utf8ByteCount(truncated));
    }

    private EngineeringWorkflowDiagnosticDto CreatePayloadDiagnostic(
        string code,
        string message,
        string sourceStep,
        string targetField)
    {
        return new EngineeringWorkflowDiagnosticDto(
            Severity: "warning",
            Code: code,
            Message: message,
            SourceStep: sourceStep,
            SourceModule: "Persistence",
            SuggestedCorrection: "Reduce payload size via compact detail level or request narrower report/trace scope.",
            TargetField: targetField);
    }

    private static int Utf8ByteCount(string value) => Encoding.UTF8.GetByteCount(value);

    private string TruncateUtf8WithMarker(string value, int maxBytes)
    {
        if (maxBytes <= 0)
        {
            return string.Empty;
        }

        var marker = _payloadLimits.TruncationMarker;
        var markerBytes = Utf8ByteCount(marker);
        if (markerBytes >= maxBytes)
        {
            return marker[..Math.Min(marker.Length, maxBytes)];
        }

        var budget = maxBytes - markerBytes;
        var runeBuffer = new StringBuilder();
        var consumedBytes = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var runeBytes = Utf8ByteCount(rune.ToString());
            if (consumedBytes + runeBytes > budget)
            {
                break;
            }

            runeBuffer.Append(rune.ToString());
            consumedBytes += runeBytes;
        }

        return string.Concat(runeBuffer.ToString(), marker);
    }

    private static IReadOnlyList<string> SortAndDistinctAssumptions(IEnumerable<string> assumptions)
    {
        return assumptions
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
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

    private sealed record ScenarioRecordBuildResult(
        EngineeringCalculationScenarioRecordDto Record,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics);

    private sealed record PayloadContent(
        string Content,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics,
        bool WasTruncated,
        int OriginalBytes,
        int StoredBytes);
}
