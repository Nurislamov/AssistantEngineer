using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations.Idempotency;
using AssistantEngineer.Api.Services.Calculations.Persistence;

namespace AssistantEngineer.Api.Services.Calculations.Workflow;

public sealed class EngineeringWorkflowSubmissionService : IEngineeringWorkflowSubmissionService
{
    private static readonly System.Text.Json.JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IEngineeringCalculationScenarioRunner _scenarioRunner;
    private readonly IEngineeringCalculationJobService _jobService;
    private readonly IEngineeringWorkflowPersistenceService _workflowPersistence;
    private readonly IEngineeringIdempotencyService _idempotencyService;
    private readonly ILogger<EngineeringWorkflowSubmissionService> _logger;

    public EngineeringWorkflowSubmissionService(
        IEngineeringCalculationScenarioRunner scenarioRunner,
        IEngineeringCalculationJobService jobService,
        IEngineeringWorkflowPersistenceService workflowPersistence,
        IEngineeringIdempotencyService idempotencyService,
        ILogger<EngineeringWorkflowSubmissionService> logger)
    {
        _scenarioRunner = scenarioRunner;
        _jobService = jobService;
        _workflowPersistence = workflowPersistence;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<EngineeringWorkflowSubmissionResult<EngineeringCalculationScenarioResultDto>> RunCalculationAsync(
        EngineeringCalculationScenarioRequestDto request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestFingerprint = EngineeringIdempotencyRequestFingerprint.Compute(request);
        var idempotencyScope = BuildIdempotencyScope("run-calculation", request.ProjectId ?? request.State.ProjectId);
        var idempotency = await _idempotencyService.EvaluateAsync(idempotencyKey, idempotencyScope, requestFingerprint, cancellationToken);

        if (idempotency.Kind == EngineeringIdempotencyEvaluationKind.Conflict)
        {
            return new EngineeringWorkflowSubmissionResult<EngineeringCalculationScenarioResultDto>(
                IsConflict: true,
                Payload: null,
                ConflictCode: idempotency.ConflictCode ?? "ENGINEERING_IDEMPOTENCY_CONFLICT",
                ConflictMessage: idempotency.ConflictMessage ?? "Idempotency conflict for engineering workflow submission.");
        }

        if (idempotency.Kind == EngineeringIdempotencyEvaluationKind.Replay)
        {
            var replay = await TryReplayRunCalculationResultAsync(idempotency.ReplayPayload, request, cancellationToken);
            if (replay is not null)
            {
                _logger.LogInformation(
                    "Engineering workflow run-calculation idempotency replay returned persisted scenario result. ProjectId={ProjectId}, ScenarioId={ScenarioId}",
                    request.ProjectId ?? request.State.ProjectId,
                    replay.ScenarioId);
                return new EngineeringWorkflowSubmissionResult<EngineeringCalculationScenarioResultDto>(IsConflict: false, Payload: replay);
            }
        }

        var result = await _scenarioRunner.RunAsync(request, cancellationToken);
        await _workflowPersistence.SaveRunScenarioAsync(request, result, cancellationToken);
        var providerInfo = _workflowPersistence.GetProviderInfo();
        var metadata = result.Metadata
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        metadata["persistence"] = providerInfo.ProviderLabel;
        metadata["persistenceProvider"] = providerInfo.Provider.ToString();
        metadata["durablePersistenceEnabled"] = providerInfo.DurableEnabled ? "true" : "false";

        var enrichedResult = result with { Metadata = metadata };
        await _idempotencyService.RecordSuccessAsync(
            idempotencyKey ?? string.Empty,
            idempotencyScope,
            requestFingerprint,
            responseJson: null,
            responseReferenceId: enrichedResult.ScenarioId,
            cancellationToken);

        return new EngineeringWorkflowSubmissionResult<EngineeringCalculationScenarioResultDto>(IsConflict: false, Payload: enrichedResult);
    }

    public async Task<EngineeringWorkflowSubmissionResult<EngineeringCalculationJobResultDto>> CreateOrRunJobAsync(
        EngineeringCalculationJobRequestDto request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestFingerprint = EngineeringIdempotencyRequestFingerprint.Compute(request);
        var idempotencyScope = BuildIdempotencyScope("jobs", request.ProjectId);
        var idempotency = await _idempotencyService.EvaluateAsync(idempotencyKey, idempotencyScope, requestFingerprint, cancellationToken);

        if (idempotency.Kind == EngineeringIdempotencyEvaluationKind.Conflict)
        {
            return new EngineeringWorkflowSubmissionResult<EngineeringCalculationJobResultDto>(
                IsConflict: true,
                Payload: null,
                ConflictCode: idempotency.ConflictCode ?? "ENGINEERING_IDEMPOTENCY_CONFLICT",
                ConflictMessage: idempotency.ConflictMessage ?? "Idempotency conflict for engineering workflow job submission.");
        }

        if (idempotency.Kind == EngineeringIdempotencyEvaluationKind.Replay &&
            !string.IsNullOrWhiteSpace(idempotency.ReplayPayload?.ResponseReferenceId))
        {
            var replayJob = await _jobService.GetJobAsync(idempotency.ReplayPayload.ResponseReferenceId, cancellationToken);
            if (replayJob is not null)
            {
                _logger.LogInformation(
                    "Engineering workflow jobs idempotency replay returned persisted job result. ProjectId={ProjectId}, JobId={JobId}",
                    request.ProjectId,
                    replayJob.JobId);
                return new EngineeringWorkflowSubmissionResult<EngineeringCalculationJobResultDto>(IsConflict: false, Payload: replayJob);
            }
        }

        var result = await _jobService.CreateOrRunJobAsync(request, cancellationToken);
        await _idempotencyService.RecordSuccessAsync(
            idempotencyKey ?? string.Empty,
            idempotencyScope,
            requestFingerprint,
            responseJson: null,
            responseReferenceId: result.JobId,
            cancellationToken);

        return new EngineeringWorkflowSubmissionResult<EngineeringCalculationJobResultDto>(IsConflict: false, Payload: result);
    }

    private async Task<EngineeringCalculationScenarioResultDto?> TryReplayRunCalculationResultAsync(
        EngineeringIdempotencyReplayPayload? replayPayload,
        EngineeringCalculationScenarioRequestDto request,
        CancellationToken cancellationToken)
    {
        if (replayPayload is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(replayPayload.ResponseJson))
        {
            try
            {
                var replay = System.Text.Json.JsonSerializer.Deserialize<EngineeringCalculationScenarioResultDto>(replayPayload.ResponseJson, DeserializeOptions);
                if (replay is not null)
                {
                    return replay;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        if (string.IsNullOrWhiteSpace(replayPayload.ResponseReferenceId))
        {
            return null;
        }

        var artifact = await _workflowPersistence.GetScenarioArtifactAsync(
            replayPayload.ResponseReferenceId,
            EngineeringCalculationArtifactKind.ScenarioResultJson,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(artifact?.Content))
        {
            try
            {
                var replay = System.Text.Json.JsonSerializer.Deserialize<EngineeringCalculationScenarioResultDto>(artifact.Content, DeserializeOptions);
                if (replay is not null)
                {
                    return replay;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // fall through to deterministic fallback
            }
        }

        var scenario = await _workflowPersistence.GetScenarioAsync(
            replayPayload.ResponseReferenceId,
            cancellationToken);
        if (scenario is null)
        {
            return null;
        }

        return new EngineeringCalculationScenarioResultDto(
            ScenarioId: scenario.ScenarioId,
            Status: scenario.Status,
            Executed: scenario.Status is EngineeringCalculationExecutionStatus.Completed or EngineeringCalculationExecutionStatus.CompletedWithWarnings or EngineeringCalculationExecutionStatus.PartiallyExecuted,
            ExecutedModules: [],
            SkippedModules: [],
            UnavailableModules: [],
            ValidationDiagnostics:
            [
                new EngineeringWorkflowDiagnosticDto(
                    Severity: "assumption",
                    Code: "ENGINEERING_IDEMPOTENCY_REPLAY_SUMMARY",
                    Message: "Idempotency replay returned persisted scenario summary reference.",
                    SourceStep: "Review",
                    SourceModule: "Idempotency",
                    SuggestedCorrection: "Use scenario artifact endpoints for full persisted result payload.")
            ],
            Assumptions:
            [
                "Idempotency replay used persisted scenario reference and did not re-execute calculation runner."
            ],
            Warnings: [],
            ModuleSummaries: new EngineeringCalculationModuleSummariesDto(
                TopologySummary: scenario.ResultSummaryJson is null ? "Persisted scenario summary unavailable." : "Persisted scenario summary is available."),
            ModuleResults: [],
            Timings: [],
            CalculationTrace: null,
            CalculationTraceSummary: null,
            EngineeringReport: null,
            ReportPreview: null,
            ReportJson: null,
            ReportMarkdown: null,
            Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["idempotencyReplay"] = "true",
                ["scenarioReferenceId"] = scenario.ScenarioId,
                ["requestScenarioId"] = request.ScenarioId
            });
    }

    private static string BuildIdempotencyScope(string action, int projectId) =>
        $"engineering-workflow:{action}:project:{projectId}";
}
