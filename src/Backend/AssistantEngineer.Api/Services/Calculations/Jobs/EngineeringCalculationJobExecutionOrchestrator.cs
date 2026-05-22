using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Jobs;
using AssistantEngineer.Api.Services.Calculations.Persistence;

namespace AssistantEngineer.Api.Services.Calculations;

internal sealed class EngineeringCalculationJobExecutionOrchestrator
{
    private readonly IEngineeringCalculationScenarioRunner _scenarioRunner;
    private readonly IEngineeringWorkflowPersistenceService _workflowPersistenceService;
    private readonly IEngineeringCalculationJobRepository _jobRepository;
    private readonly EngineeringCalculationJobPayloadCodec _payloadCodec;
    private readonly EngineeringCalculationJobStatusTransitionPolicy _statusTransitionPolicy;
    private readonly EngineeringCalculationJobEventRecorder _eventRecorder;
    private readonly ILogger _logger;

    public EngineeringCalculationJobExecutionOrchestrator(
        IEngineeringCalculationScenarioRunner scenarioRunner,
        IEngineeringWorkflowPersistenceService workflowPersistenceService,
        IEngineeringCalculationJobRepository jobRepository,
        EngineeringCalculationJobPayloadCodec payloadCodec,
        EngineeringCalculationJobStatusTransitionPolicy statusTransitionPolicy,
        EngineeringCalculationJobEventRecorder eventRecorder,
        ILogger logger)
    {
        _scenarioRunner = scenarioRunner;
        _workflowPersistenceService = workflowPersistenceService;
        _jobRepository = jobRepository;
        _payloadCodec = payloadCodec;
        _statusTransitionPolicy = statusTransitionPolicy;
        _eventRecorder = eventRecorder;
        _logger = logger;
    }

    public async Task<EngineeringCalculationJobExecutionResult> ExecuteAsync(
        EngineeringCalculationJobRecordDto job,
        EngineeringCalculationJobRequestDto request,
        List<string> assumptions,
        List<string> warnings,
        List<EngineeringWorkflowDiagnosticDto> diagnostics,
        DateTimeOffset startedAt,
        bool persistRunningState,
        CancellationToken cancellationToken)
    {
        if (persistRunningState)
        {
            job = _statusTransitionPolicy.MoveToRunning(job, startedAt);
            job = await _jobRepository.UpdateAsync(job, cancellationToken);
            await _eventRecorder.AppendAsync(
                job,
                EngineeringCalculationJobStatus.Running,
                "Calculation job started.",
                null,
                job.ProgressPercent,
                diagnostics,
                startedAt,
                cancellationToken);
        }

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

            var finalStatus = _statusTransitionPolicy.MapScenarioStatus(scenarioResult.Status);
            var normalizedDiagnostics = _payloadCodec.SortAndDistinctDiagnostics(diagnostics);
            var duration = Math.Max(0.0, (completedAt - startedAt).TotalMilliseconds);

            job = _statusTransitionPolicy.MoveToCompleted(
                job,
                finalStatus,
                _payloadCodec.Serialize(scenarioResult),
                _payloadCodec.Serialize(normalizedDiagnostics),
                completedAt,
                duration);
            job = await _jobRepository.UpdateAsync(job, cancellationToken);

            await _eventRecorder.AppendAsync(
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

            return new EngineeringCalculationJobExecutionResult(
                Job: job,
                ScenarioResultSummary: scenarioResult,
                Diagnostics: normalizedDiagnostics,
                Assumptions: _payloadCodec.SortAndDistinctText(assumptions),
                Warnings: _payloadCodec.SortAndDistinctText(warnings));
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

            var normalizedDiagnostics = _payloadCodec.SortAndDistinctDiagnostics(diagnostics);
            var duration = Math.Max(0.0, (failedAt - startedAt).TotalMilliseconds);

            job = _statusTransitionPolicy.MoveToFailedExecution(
                job,
                _payloadCodec.Serialize(normalizedDiagnostics),
                failedAt,
                duration);
            job = await _jobRepository.UpdateAsync(job, cancellationToken);

            await _eventRecorder.AppendAsync(
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

            return new EngineeringCalculationJobExecutionResult(
                Job: job,
                ScenarioResultSummary: null,
                Diagnostics: normalizedDiagnostics,
                Assumptions: _payloadCodec.SortAndDistinctText(assumptions),
                Warnings: _payloadCodec.SortAndDistinctText(warnings));
        }
    }
}

internal sealed record EngineeringCalculationJobExecutionResult(
    EngineeringCalculationJobRecordDto Job,
    EngineeringCalculationScenarioResultDto? ScenarioResultSummary,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings);
