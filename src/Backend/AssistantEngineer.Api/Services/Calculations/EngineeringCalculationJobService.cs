using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationJobService : IEngineeringCalculationJobService
{
    private const string DirectQueuedExecutorWorkerId = "direct-queued-executor";
    private static readonly TimeSpan DefaultDirectClaimLease = TimeSpan.FromMinutes(5);

    private readonly IEngineeringWorkflowPersistenceService _workflowPersistenceService;
    private readonly IEngineeringCalculationJobRepository _jobRepository;
    private readonly IEngineeringCalculationJobEventRepository _jobEventRepository;
    private readonly ILogger<EngineeringCalculationJobService> _logger;
    private readonly EngineeringCalculationJobPayloadCodec _payloadCodec;
    private readonly EngineeringCalculationJobStatusTransitionPolicy _statusTransitionPolicy;
    private readonly EngineeringCalculationJobEventRecorder _eventRecorder;
    private readonly EngineeringCalculationJobExecutionOrchestrator _executionOrchestrator;

    public EngineeringCalculationJobService(
        IEngineeringCalculationScenarioRunner scenarioRunner,
        IEngineeringWorkflowPersistenceService workflowPersistenceService,
        IEngineeringCalculationJobRepository jobRepository,
        IEngineeringCalculationJobEventRepository jobEventRepository,
        EngineeringCalculationJobPayloadCodec payloadCodec,
        EngineeringCalculationJobStatusTransitionPolicy statusTransitionPolicy,
        EngineeringCalculationJobEventRecorder eventRecorder,
        ILogger<EngineeringCalculationJobService> logger)
    {
        _workflowPersistenceService = workflowPersistenceService;
        _jobRepository = jobRepository;
        _jobEventRepository = jobEventRepository;
        _logger = logger;

        _payloadCodec = payloadCodec;
        _statusTransitionPolicy = statusTransitionPolicy;
        _eventRecorder = eventRecorder;
        _executionOrchestrator = new EngineeringCalculationJobExecutionOrchestrator(
            scenarioRunner,
            _workflowPersistenceService,
            _jobRepository,
            _payloadCodec,
            _statusTransitionPolicy,
            _eventRecorder,
            _logger);
    }

    public async Task<EngineeringCalculationJobResultDto> CreateOrRunJobAsync(
        EngineeringCalculationJobRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ScenarioRequest);
        ArgumentNullException.ThrowIfNull(request.ScenarioRequest.State);

        var timestamp = request.DeterministicTimestampUtc ?? DateTimeOffset.UtcNow;
        var scenarioId = ResolveScenarioId(request);
        var jobId = ResolveJobId(request, scenarioId);
        var diagnostics = new List<EngineeringWorkflowDiagnosticDto>();
        var assumptions = new List<string>
        {
            "Job lifecycle orchestration reuses existing scenario runner and does not introduce new engineering physics."
        };
        var warnings = new List<string>();

        _logger.LogInformation(
            "Engineering calculation job create requested. JobId={JobId}, ScenarioId={ScenarioId}, ProjectId={ProjectId}, ExecutionMode={ExecutionMode}",
            jobId,
            scenarioId,
            request.ProjectId,
            request.ExecutionMode);

        var job = new EngineeringCalculationJobRecordDto(
            JobId: jobId,
            ProjectId: request.ProjectId,
            ScenarioId: scenarioId,
            Status: EngineeringCalculationJobStatus.Created,
            ExecutionMode: request.ExecutionMode,
            RequestJson: _payloadCodec.Serialize(request),
            ResultSummaryJson: null,
            DiagnosticsJson: null,
            ProgressPercent: 0,
            CurrentStep: "Created",
            CreatedAtUtc: timestamp,
            QueuedAtUtc: null,
            StartedAtUtc: null,
            CompletedAtUtc: null,
            UpdatedAtUtc: timestamp,
            DurationMilliseconds: null,
            RetryCount: 0,
            CancellationRequested: false);

        job = await _jobRepository.CreateAsync(job, cancellationToken);
        await _eventRecorder.AppendAsync(job, EngineeringCalculationJobStatus.Created, "Calculation job created.", null, 0, diagnostics, timestamp, cancellationToken);

        job = _statusTransitionPolicy.MoveToQueued(job, timestamp);
        job = await _jobRepository.UpdateAsync(job, cancellationToken);
        await _eventRecorder.AppendAsync(job, EngineeringCalculationJobStatus.Queued, "Calculation job queued.", null, job.ProgressPercent, diagnostics, timestamp.AddMilliseconds(1), cancellationToken);

        if (request.ExecutionMode == EngineeringCalculationJobExecutionMode.Queued)
        {
            assumptions.Add("Queued execution mode stores the job for the background worker; it no longer reports a fake completed or stranded execution path.");
            _logger.LogInformation(
                "Engineering calculation job queued without immediate execution. JobId={JobId}, ScenarioId={ScenarioId}",
                job.JobId,
                job.ScenarioId);

            return await BuildJobResultAsync(
                job,
                scenarioResultSummary: null,
                diagnostics: _payloadCodec.SortAndDistinctDiagnostics(diagnostics),
                assumptions: assumptions,
                warnings: warnings,
                cancellationToken: cancellationToken);
        }

        var executionResult = await _executionOrchestrator.ExecuteAsync(
            job,
            request,
            assumptions,
            warnings,
            diagnostics,
            timestamp.AddMilliseconds(2),
            persistRunningState: true,
            cancellationToken);

        return await BuildJobResultAsync(
            executionResult.Job,
            executionResult.ScenarioResultSummary,
            executionResult.Diagnostics,
            executionResult.Assumptions,
            executionResult.Warnings,
            cancellationToken);
    }

    public async Task<EngineeringCalculationJobResultDto?> ExecuteQueuedJobAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var claimedJob = await _jobRepository.TryClaimQueuedJobAsync(
            jobId,
            workerId: DirectQueuedExecutorWorkerId,
            leaseDuration: DefaultDirectClaimLease,
            cancellationToken);

        if (claimedJob is null)
        {
            var existing = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
            if (existing is null)
            {
                return null;
            }

            var existingDiagnostics = _payloadCodec.DeserializeDiagnostics(existing.DiagnosticsJson);
            _logger.LogInformation(
                "Engineering queued job execution skipped because queued claim failed. JobId={JobId}, Status={Status}, ClaimedByWorkerId={ClaimedByWorkerId}",
                existing.JobId,
                existing.Status,
                existing.ClaimedByWorkerId ?? "n/a");

            return await BuildJobResultAsync(
                existing,
                _payloadCodec.DeserializeScenarioResult(existing.ResultSummaryJson),
                existingDiagnostics,
                assumptions: [],
                warnings: [],
                cancellationToken);
        }

        return await ExecuteClaimedJobAsync(claimedJob.JobId, DirectQueuedExecutorWorkerId, cancellationToken);
    }

    public async Task<EngineeringCalculationJobResultDto?> ExecuteClaimedJobAsync(
        string jobId,
        string workerId,
        CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var diagnostics = _payloadCodec.DeserializeDiagnostics(job.DiagnosticsJson).ToList();
        var request = _payloadCodec.DeserializeJobRequest(job.RequestJson);
        if (request is null)
        {
            var failedAt = DateTimeOffset.UtcNow;
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "CALCULATION_JOB_REQUEST_DESERIALIZATION_FAILED",
                Message: "Queued job request payload could not be deserialized.",
                SourceStep: "Review",
                SuggestedCorrection: "Recreate the job from the original scenario request."));

            var normalizedDiagnostics = _payloadCodec.SortAndDistinctDiagnostics(diagnostics);
            job = _statusTransitionPolicy.MoveToFailedExecution(
                job,
                _payloadCodec.Serialize(normalizedDiagnostics),
                failedAt,
                durationMilliseconds: 0);

            job = await _jobRepository.UpdateAsync(job, cancellationToken);
            await _eventRecorder.AppendAsync(
                job,
                EngineeringCalculationJobStatus.FailedExecution,
                "Calculation job failed before execution because its request payload is invalid.",
                null,
                100,
                diagnostics,
                failedAt,
                cancellationToken);

            return await BuildJobResultAsync(job, null, normalizedDiagnostics, [], [], cancellationToken);
        }

        if (!_statusTransitionPolicy.IsRunning(job.Status))
        {
            _logger.LogInformation(
                "Engineering queued job execution skipped because state is not running. JobId={JobId}, Status={Status}",
                job.JobId,
                job.Status);

            return await BuildJobResultAsync(
                job,
                _payloadCodec.DeserializeScenarioResult(job.ResultSummaryJson),
                diagnostics,
                assumptions: [],
                warnings: [],
                cancellationToken);
        }

        if (!_statusTransitionPolicy.IsClaimedByWorker(job, workerId))
        {
            _logger.LogInformation(
                "Engineering queued job execution skipped because running claim owner differs. JobId={JobId}, ClaimedByWorkerId={ClaimedByWorkerId}, RequestedWorkerId={RequestedWorkerId}",
                job.JobId,
                job.ClaimedByWorkerId ?? "n/a",
                workerId);

            return await BuildJobResultAsync(
                job,
                _payloadCodec.DeserializeScenarioResult(job.ResultSummaryJson),
                diagnostics,
                assumptions: [],
                warnings: [],
                cancellationToken);
        }

        if (job.CancellationRequested)
        {
            var cancelledAt = DateTimeOffset.UtcNow;
            job = _statusTransitionPolicy.MoveToCancelled(job, cancelledAt);
            job = await _jobRepository.UpdateAsync(job, cancellationToken);
            await _eventRecorder.AppendAsync(
                job,
                EngineeringCalculationJobStatus.Cancelled,
                "Queued job was cancelled before worker execution.",
                null,
                100,
                diagnostics,
                cancelledAt,
                cancellationToken);

            _logger.LogInformation(
                "Engineering queued job cancelled before execution. JobId={JobId}",
                job.JobId);

            return await BuildJobResultAsync(job, null, diagnostics, [], [], cancellationToken);
        }

        var startedAt = job.StartedAtUtc ?? DateTimeOffset.UtcNow;
        await _eventRecorder.AppendAsync(
            job,
            EngineeringCalculationJobStatus.Running,
            "Calculation job started.",
            null,
            job.ProgressPercent,
            diagnostics,
            startedAt,
            cancellationToken);

        var assumptions = new List<string>
        {
            "Queued job was picked up by the background worker; execution still uses the existing scenario runner."
        };
        var warnings = new List<string>();

        var executionResult = await _executionOrchestrator.ExecuteAsync(
            job,
            request,
            assumptions,
            warnings,
            diagnostics,
            startedAt,
            persistRunningState: false,
            cancellationToken);

        return await BuildJobResultAsync(
            executionResult.Job,
            executionResult.ScenarioResultSummary,
            executionResult.Diagnostics,
            executionResult.Assumptions,
            executionResult.Warnings,
            cancellationToken);
    }

    public async Task<EngineeringCalculationJobResultDto?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var scenarioResult = _payloadCodec.DeserializeScenarioResult(job.ResultSummaryJson);
        var diagnostics = _payloadCodec.DeserializeDiagnostics(job.DiagnosticsJson);
        return await BuildJobResultAsync(
            job,
            scenarioResult,
            diagnostics,
            assumptions: [],
            warnings: [],
            cancellationToken);
    }

    public async Task<IReadOnlyList<EngineeringCalculationJobResultDto>> ListProjectJobsAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var jobs = await _jobRepository.ListByProjectIdAsync(projectId, cancellationToken);
        var results = new List<EngineeringCalculationJobResultDto>(jobs.Count);
        foreach (var job in jobs)
        {
            var scenarioResult = _payloadCodec.DeserializeScenarioResult(job.ResultSummaryJson);
            var diagnostics = _payloadCodec.DeserializeDiagnostics(job.DiagnosticsJson);
            results.Add(await BuildJobResultAsync(
                job,
                scenarioResult,
                diagnostics,
                assumptions: [],
                warnings: [],
                cancellationToken));
        }

        return results
            .OrderByDescending(item => item.QueuedAtUtc)
            .ThenBy(item => item.JobId, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<IReadOnlyList<EngineeringCalculationJobEventDto>> ListJobEventsAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var records = await _jobEventRepository.ListByJobIdAsync(jobId, cancellationToken);
        return records.Select(_eventRecorder.MapToDto).ToArray();
    }

    public async Task<EngineeringCalculationJobResultDto?> CancelJobAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var timestamp = DateTimeOffset.UtcNow;
        var diagnostics = _payloadCodec.DeserializeDiagnostics(job.DiagnosticsJson).ToList();

        if (_statusTransitionPolicy.IsReadyForCancel(job.Status))
        {
            job = _statusTransitionPolicy.MoveToCancelled(job, timestamp);
            await _eventRecorder.AppendAsync(
                job,
                EngineeringCalculationJobStatus.Cancelled,
                "Queued job cancelled.",
                null,
                job.ProgressPercent,
                diagnostics,
                timestamp,
                cancellationToken);
            _logger.LogInformation(
                "Engineering calculation job cancelled. JobId={JobId}, ProjectId={ProjectId}",
                job.JobId,
                job.ProjectId);
        }
        else if (_statusTransitionPolicy.IsRunning(job.Status))
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "CALCULATION_JOB_CANCEL_RUNNING_NOT_SUPPORTED",
                Message: "Cancellation was requested while job is running, but immediate running cancellation is not supported in foundation mode.",
                SourceStep: "Review"));

            job = _statusTransitionPolicy.MoveToCancelRequested(job, timestamp);
            await _eventRecorder.AppendAsync(
                job,
                EngineeringCalculationJobStatus.CancelRequested,
                "Cancellation requested for running job.",
                null,
                job.ProgressPercent,
                diagnostics,
                timestamp,
                cancellationToken);
            _logger.LogWarning(
                "Engineering calculation job cancellation requested while running. JobId={JobId}",
                job.JobId);
        }
        else
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "info",
                Code: "CALCULATION_JOB_CANCEL_NOOP",
                Message: "Job is already in terminal lifecycle state.",
                SourceStep: "Review"));
        }

        job = job with
        {
            DiagnosticsJson = _payloadCodec.Serialize(_payloadCodec.SortAndDistinctDiagnostics(diagnostics))
        };
        job = await _jobRepository.UpdateAsync(job, cancellationToken);

        return await BuildJobResultAsync(
            job,
            _payloadCodec.DeserializeScenarioResult(job.ResultSummaryJson),
            _payloadCodec.SortAndDistinctDiagnostics(diagnostics),
            assumptions: [],
            warnings: [],
            cancellationToken);
    }

    private async Task<EngineeringCalculationJobResultDto> BuildJobResultAsync(
        EngineeringCalculationJobRecordDto job,
        EngineeringCalculationScenarioResultDto? scenarioResultSummary,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        var events = await ListJobEventsAsync(job.JobId, cancellationToken);
        var artifacts = await _workflowPersistenceService.ListScenarioArtifactsAsync(job.ScenarioId, cancellationToken);
        var providerInfo = _workflowPersistenceService.GetProviderInfo();

        var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["persistence"] = providerInfo.ProviderLabel,
            ["persistenceProvider"] = providerInfo.Provider.ToString(),
            ["durablePersistenceEnabled"] = providerInfo.DurableEnabled ? "true" : "false",
            ["executionMode"] = job.ExecutionMode.ToString(),
            ["queuedWorkerSupported"] = "true"
        };

        return new EngineeringCalculationJobResultDto(
            JobId: job.JobId,
            ProjectId: job.ProjectId,
            ScenarioId: job.ScenarioId,
            Status: job.Status,
            ProgressPercent: job.ProgressPercent,
            CurrentStep: job.CurrentStep,
            QueuedAtUtc: job.QueuedAtUtc ?? job.CreatedAtUtc,
            StartedAtUtc: job.StartedAtUtc,
            CompletedAtUtc: job.CompletedAtUtc,
            DurationMilliseconds: job.DurationMilliseconds,
            ScenarioResultSummary: scenarioResultSummary,
            Diagnostics: _payloadCodec.SortAndDistinctDiagnostics(diagnostics),
            Assumptions: _payloadCodec.SortAndDistinctText(assumptions),
            Warnings: _payloadCodec.SortAndDistinctText(warnings),
            PersistedArtifactReferences: artifacts,
            HistoryEvents: events,
            Metadata: metadata);
    }

    private static string ResolveScenarioId(EngineeringCalculationJobRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.ScenarioId))
        {
            return request.ScenarioId!;
        }

        if (!string.IsNullOrWhiteSpace(request.ScenarioRequest.ScenarioId))
        {
            return request.ScenarioRequest.ScenarioId;
        }

        return $"wf-scenario-{request.ProjectId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

    private static string ResolveJobId(EngineeringCalculationJobRequestDto request, string scenarioId)
    {
        if (!string.IsNullOrWhiteSpace(request.JobId))
        {
            return request.JobId!;
        }

        return $"job-{scenarioId}";
    }
}
