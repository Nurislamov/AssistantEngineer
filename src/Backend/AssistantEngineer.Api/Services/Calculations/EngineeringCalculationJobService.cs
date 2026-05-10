using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationJobService : IEngineeringCalculationJobService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly IEngineeringCalculationScenarioRunner _scenarioRunner;
    private readonly IEngineeringWorkflowPersistenceService _workflowPersistenceService;
    private readonly IEngineeringCalculationJobRepository _jobRepository;
    private readonly IEngineeringCalculationJobEventRepository _jobEventRepository;
    private readonly ILogger<EngineeringCalculationJobService> _logger;

    public EngineeringCalculationJobService(
        IEngineeringCalculationScenarioRunner scenarioRunner,
        IEngineeringWorkflowPersistenceService workflowPersistenceService,
        IEngineeringCalculationJobRepository jobRepository,
        IEngineeringCalculationJobEventRepository jobEventRepository,
        ILogger<EngineeringCalculationJobService> logger)
    {
        _scenarioRunner = scenarioRunner;
        _workflowPersistenceService = workflowPersistenceService;
        _jobRepository = jobRepository;
        _jobEventRepository = jobEventRepository;
        _logger = logger;
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
        var progress = 0;
        var currentStep = "Created";

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
            RequestJson: JsonSerializer.Serialize(request, JsonOptions),
            ResultSummaryJson: null,
            DiagnosticsJson: null,
            ProgressPercent: progress,
            CurrentStep: currentStep,
            CreatedAtUtc: timestamp,
            QueuedAtUtc: null,
            StartedAtUtc: null,
            CompletedAtUtc: null,
            UpdatedAtUtc: timestamp,
            DurationMilliseconds: null,
            RetryCount: 0,
            CancellationRequested: false);

        job = await _jobRepository.CreateAsync(job, cancellationToken);
        await AppendEventAsync(job, EngineeringCalculationJobStatus.Created, "Calculation job created.", null, progress, diagnostics, timestamp, cancellationToken);

        job = job with
        {
            Status = EngineeringCalculationJobStatus.Queued,
            ProgressPercent = 5,
            CurrentStep = "Queued",
            QueuedAtUtc = timestamp,
            UpdatedAtUtc = timestamp
        };
        job = await _jobRepository.UpdateAsync(job, cancellationToken);
        await AppendEventAsync(job, EngineeringCalculationJobStatus.Queued, "Calculation job queued.", null, job.ProgressPercent, diagnostics, timestamp.AddMilliseconds(1), cancellationToken);

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
                diagnostics: SortAndDistinctDiagnostics(diagnostics),
                assumptions: assumptions,
                warnings: warnings,
                cancellationToken: cancellationToken);
        }

        return await ExecuteJobAsync(
            job,
            request,
            assumptions,
            warnings,
            diagnostics,
            timestamp.AddMilliseconds(2),
            cancellationToken);
    }

    public async Task<EngineeringCalculationJobResultDto?> ExecuteQueuedJobAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var diagnostics = DeserializeDiagnostics(job.DiagnosticsJson).ToList();
        var request = DeserializeJobRequest(job.RequestJson);
        if (request is null)
        {
            var failedAt = DateTimeOffset.UtcNow;
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "CALCULATION_JOB_REQUEST_DESERIALIZATION_FAILED",
                Message: "Queued job request payload could not be deserialized.",
                SourceStep: "Review",
                SuggestedCorrection: "Recreate the job from the original scenario request."));

            job = job with
            {
                Status = EngineeringCalculationJobStatus.FailedExecution,
                DiagnosticsJson = JsonSerializer.Serialize(SortAndDistinctDiagnostics(diagnostics), JsonOptions),
                ProgressPercent = 100,
                CurrentStep = "Failed",
                CompletedAtUtc = failedAt,
                UpdatedAtUtc = failedAt,
                DurationMilliseconds = 0
            };
            job = await _jobRepository.UpdateAsync(job, cancellationToken);
            await AppendEventAsync(job, EngineeringCalculationJobStatus.FailedExecution, "Calculation job failed before execution because its request payload is invalid.", null, 100, diagnostics, failedAt, cancellationToken);
            return await BuildJobResultAsync(job, null, SortAndDistinctDiagnostics(diagnostics), [], [], cancellationToken);
        }

        if (job.Status is not (EngineeringCalculationJobStatus.Queued or EngineeringCalculationJobStatus.RetryScheduled))
        {
            _logger.LogInformation(
                "Engineering queued job execution skipped because state is not queued. JobId={JobId}, Status={Status}",
                job.JobId,
                job.Status);
            return await BuildJobResultAsync(
                job,
                DeserializeScenarioResult(job.ResultSummaryJson),
                diagnostics,
                assumptions: [],
                warnings: [],
                cancellationToken);
        }

        if (job.CancellationRequested)
        {
            var cancelledAt = DateTimeOffset.UtcNow;
            job = job with
            {
                Status = EngineeringCalculationJobStatus.Cancelled,
                ProgressPercent = 100,
                CurrentStep = "Cancelled",
                CompletedAtUtc = cancelledAt,
                UpdatedAtUtc = cancelledAt
            };
            job = await _jobRepository.UpdateAsync(job, cancellationToken);
            await AppendEventAsync(job, EngineeringCalculationJobStatus.Cancelled, "Queued job was cancelled before worker execution.", null, 100, diagnostics, cancelledAt, cancellationToken);
            _logger.LogInformation(
                "Engineering queued job cancelled before execution. JobId={JobId}",
                job.JobId);
            return await BuildJobResultAsync(job, null, diagnostics, [], [], cancellationToken);
        }

        var assumptions = new List<string>
        {
            "Queued job was picked up by the background worker; execution still uses the existing scenario runner."
        };
        var warnings = new List<string>();

        return await ExecuteJobAsync(
            job,
            request,
            assumptions,
            warnings,
            diagnostics,
            DateTimeOffset.UtcNow,
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

        var scenarioResult = DeserializeScenarioResult(job.ResultSummaryJson);
        var diagnostics = DeserializeDiagnostics(job.DiagnosticsJson);
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
            var scenarioResult = DeserializeScenarioResult(job.ResultSummaryJson);
            var diagnostics = DeserializeDiagnostics(job.DiagnosticsJson);
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
        return records.Select(MapJobEvent).ToArray();
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
        var diagnostics = DeserializeDiagnostics(job.DiagnosticsJson).ToList();
        var nextStatus = job.Status;
        var nextStep = job.CurrentStep;

        if (job.Status is EngineeringCalculationJobStatus.Created or EngineeringCalculationJobStatus.Queued or EngineeringCalculationJobStatus.RetryScheduled)
        {
            nextStatus = EngineeringCalculationJobStatus.Cancelled;
            nextStep = "Cancelled";
            job = job with
            {
                Status = nextStatus,
                ProgressPercent = Math.Max(job.ProgressPercent, 100),
                CurrentStep = nextStep,
                CompletedAtUtc = timestamp,
                UpdatedAtUtc = timestamp,
                CancellationRequested = true
            };

            await AppendEventAsync(job, EngineeringCalculationJobStatus.Cancelled, "Queued job cancelled.", null, job.ProgressPercent, diagnostics, timestamp, cancellationToken);
            _logger.LogInformation(
                "Engineering calculation job cancelled. JobId={JobId}, ProjectId={ProjectId}",
                job.JobId,
                job.ProjectId);
        }
        else if (job.Status == EngineeringCalculationJobStatus.Running)
        {
            nextStatus = EngineeringCalculationJobStatus.CancelRequested;
            nextStep = "CancelRequested";
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "CALCULATION_JOB_CANCEL_RUNNING_NOT_SUPPORTED",
                Message: "Cancellation was requested while job is running, but immediate running cancellation is not supported in foundation mode.",
                SourceStep: "Review"));
            job = job with
            {
                Status = nextStatus,
                CurrentStep = nextStep,
                UpdatedAtUtc = timestamp,
                CancellationRequested = true
            };

            await AppendEventAsync(job, EngineeringCalculationJobStatus.CancelRequested, "Cancellation requested for running job.", null, job.ProgressPercent, diagnostics, timestamp, cancellationToken);
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
            DiagnosticsJson = JsonSerializer.Serialize(SortAndDistinctDiagnostics(diagnostics), JsonOptions)
        };
        job = await _jobRepository.UpdateAsync(job, cancellationToken);

        return await BuildJobResultAsync(
            job,
            DeserializeScenarioResult(job.ResultSummaryJson),
            SortAndDistinctDiagnostics(diagnostics),
            assumptions: [],
            warnings: [],
            cancellationToken);
    }

    private async Task<EngineeringCalculationJobResultDto> ExecuteJobAsync(
        EngineeringCalculationJobRecordDto job,
        EngineeringCalculationJobRequestDto request,
        List<string> assumptions,
        List<string> warnings,
        List<EngineeringWorkflowDiagnosticDto> diagnostics,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        job = job with
        {
            Status = EngineeringCalculationJobStatus.Running,
            ProgressPercent = 25,
            CurrentStep = "Running",
            StartedAtUtc = startedAt,
            UpdatedAtUtc = startedAt
        };
        job = await _jobRepository.UpdateAsync(job, cancellationToken);
        await AppendEventAsync(job, EngineeringCalculationJobStatus.Running, "Calculation job started.", null, job.ProgressPercent, diagnostics, startedAt, cancellationToken);
        _logger.LogInformation(
            "Engineering calculation job started. JobId={JobId}, ScenarioId={ScenarioId}, ProjectId={ProjectId}",
            job.JobId,
            job.ScenarioId,
            job.ProjectId);

        try
        {
            var scenarioRequest = request.ScenarioRequest with
            {
                ScenarioId = job.ScenarioId,
                ProjectId = job.ProjectId,
                IncludeTrace = request.IncludeTrace,
                IncludeReport = request.IncludeReport,
                ReportFormats = request.RequestedReportFormats ?? request.ScenarioRequest.ReportFormats
            };

            if (request.ExecutionMode == EngineeringCalculationJobExecutionMode.DryRun)
            {
                scenarioRequest = scenarioRequest with { ExecutionMode = EngineeringCalculationExecutionMode.DryRun };
                assumptions.Add("Job execution mode DryRun maps to scenario DryRun path.");
            }
            else if (request.ExecutionMode == EngineeringCalculationJobExecutionMode.ValidateOnly)
            {
                scenarioRequest = scenarioRequest with { ExecutionMode = EngineeringCalculationExecutionMode.ValidateOnly };
                assumptions.Add("Job execution mode ValidateOnly maps to scenario ValidateOnly path.");
            }

            var scenarioResult = await _scenarioRunner.RunAsync(scenarioRequest, cancellationToken);
            diagnostics.AddRange(scenarioResult.ValidationDiagnostics);
            assumptions.AddRange(scenarioResult.Assumptions);
            warnings.AddRange(scenarioResult.Warnings);

            await _workflowPersistenceService.SaveRunScenarioAsync(scenarioRequest, scenarioResult, cancellationToken);

            var completedAt = request.DeterministicTimestampUtc ?? DateTimeOffset.UtcNow;
            if (completedAt < startedAt)
            {
                completedAt = startedAt;
            }

            var finalStatus = MapScenarioStatus(scenarioResult.Status);
            var normalizedDiagnostics = SortAndDistinctDiagnostics(diagnostics);
            var duration = Math.Max(0.0, (completedAt - startedAt).TotalMilliseconds);

            job = job with
            {
                Status = finalStatus,
                ResultSummaryJson = JsonSerializer.Serialize(scenarioResult, JsonOptions),
                DiagnosticsJson = JsonSerializer.Serialize(normalizedDiagnostics, JsonOptions),
                ProgressPercent = 100,
                CurrentStep = "Completed",
                CompletedAtUtc = completedAt,
                UpdatedAtUtc = completedAt,
                DurationMilliseconds = duration
            };
            job = await _jobRepository.UpdateAsync(job, cancellationToken);

            await AppendEventAsync(
                job,
                finalStatus,
                $"Calculation job completed with status `{finalStatus}`.",
                null,
                100,
                normalizedDiagnostics,
                completedAt,
                cancellationToken);
            _logger.LogInformation(
                "Engineering calculation job completed. JobId={JobId}, ScenarioId={ScenarioId}, Status={Status}, DurationMs={DurationMs}, DiagnosticsCount={DiagnosticsCount}",
                job.JobId,
                job.ScenarioId,
                finalStatus,
                duration,
                normalizedDiagnostics.Count);

            return await BuildJobResultAsync(
                job,
                scenarioResult,
                normalizedDiagnostics,
                assumptions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                warnings.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                cancellationToken);
        }
        catch (Exception exception)
        {
            var failedAt = request.DeterministicTimestampUtc ?? DateTimeOffset.UtcNow;
            if (failedAt < startedAt)
            {
                failedAt = startedAt;
            }

            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "CALCULATION_JOB_EXECUTION_FAILED",
                Message: exception.Message,
                SourceStep: "Review",
                SuggestedCorrection: "Inspect scenario inputs and backend runner logs, then retry."));

            var normalizedDiagnostics = SortAndDistinctDiagnostics(diagnostics);
            var duration = Math.Max(0.0, (failedAt - startedAt).TotalMilliseconds);

            job = job with
            {
                Status = EngineeringCalculationJobStatus.FailedExecution,
                DiagnosticsJson = JsonSerializer.Serialize(normalizedDiagnostics, JsonOptions),
                ProgressPercent = 100,
                CurrentStep = "Failed",
                CompletedAtUtc = failedAt,
                UpdatedAtUtc = failedAt,
                DurationMilliseconds = duration
            };
            job = await _jobRepository.UpdateAsync(job, cancellationToken);

            await AppendEventAsync(
                job,
                EngineeringCalculationJobStatus.FailedExecution,
                "Calculation job failed during execution.",
                null,
                100,
                normalizedDiagnostics,
                failedAt,
                cancellationToken);
            _logger.LogError(
                exception,
                "Engineering calculation job failed. JobId={JobId}, ScenarioId={ScenarioId}, DurationMs={DurationMs}, DiagnosticsCount={DiagnosticsCount}",
                job.JobId,
                job.ScenarioId,
                duration,
                normalizedDiagnostics.Count);

            return await BuildJobResultAsync(
                job,
                scenarioResultSummary: null,
                diagnostics: normalizedDiagnostics,
                assumptions: assumptions,
                warnings: warnings,
                cancellationToken: cancellationToken);
        }
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
        var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["persistence"] = _workflowPersistenceService.GetProviderInfo().ProviderLabel,
            ["persistenceProvider"] = _workflowPersistenceService.GetProviderInfo().Provider.ToString(),
            ["durablePersistenceEnabled"] = _workflowPersistenceService.GetProviderInfo().DurableEnabled ? "true" : "false",
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
            Diagnostics: SortAndDistinctDiagnostics(diagnostics),
            Assumptions: assumptions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            Warnings: warnings.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            PersistedArtifactReferences: artifacts,
            HistoryEvents: events,
            Metadata: metadata);
    }

    private async Task AppendEventAsync(
        EngineeringCalculationJobRecordDto job,
        EngineeringCalculationJobStatus status,
        string message,
        string? moduleKind,
        int? progressPercent,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var eventId = $"{job.JobId}:{status}:{timestamp.ToUnixTimeMilliseconds()}";
        var eventRecord = new EngineeringCalculationJobEventRecordDto(
            EventId: eventId,
            JobId: job.JobId,
            ScenarioId: job.ScenarioId,
            ProjectId: job.ProjectId,
            Status: status,
            EventKind: status.ToString(),
            Message: moduleKind is null ? message : $"{message} ({moduleKind})",
            DiagnosticsJson: JsonSerializer.Serialize(SortAndDistinctDiagnostics(diagnostics), JsonOptions),
            ProgressPercent: progressPercent,
            CreatedAtUtc: timestamp);

        await _jobEventRepository.AppendAsync(eventRecord, cancellationToken);
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

    private static EngineeringCalculationJobStatus MapScenarioStatus(EngineeringCalculationExecutionStatus status)
    {
        return status switch
        {
            EngineeringCalculationExecutionStatus.Completed => EngineeringCalculationJobStatus.Completed,
            EngineeringCalculationExecutionStatus.CompletedWithWarnings => EngineeringCalculationJobStatus.CompletedWithWarnings,
            EngineeringCalculationExecutionStatus.FailedValidation => EngineeringCalculationJobStatus.FailedValidation,
            EngineeringCalculationExecutionStatus.FailedExecution => EngineeringCalculationJobStatus.FailedExecution,
            EngineeringCalculationExecutionStatus.PartiallyExecuted => EngineeringCalculationJobStatus.CompletedWithWarnings,
            EngineeringCalculationExecutionStatus.Prepared => EngineeringCalculationJobStatus.CompletedWithWarnings,
            _ => EngineeringCalculationJobStatus.NotSupported
        };
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

    private static EngineeringCalculationJobRequestDto? DeserializeJobRequest(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<EngineeringCalculationJobRequestDto>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static EngineeringCalculationScenarioResultDto? DeserializeScenarioResult(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<EngineeringCalculationScenarioResultDto>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> DeserializeDiagnostics(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<EngineeringWorkflowDiagnosticDto>>(raw, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static EngineeringCalculationJobEventDto MapJobEvent(EngineeringCalculationJobEventRecordDto source)
    {
        return new EngineeringCalculationJobEventDto(
            EventId: source.EventId,
            JobId: source.JobId,
            ScenarioId: source.ScenarioId,
            Status: source.Status,
            Message: source.Message,
            ModuleKind: null,
            ProgressPercent: source.ProgressPercent,
            Diagnostics: DeserializeDiagnostics(source.DiagnosticsJson),
            CreatedAtUtc: source.CreatedAtUtc);
    }
}
