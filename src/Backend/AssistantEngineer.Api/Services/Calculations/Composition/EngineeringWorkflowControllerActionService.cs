using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;

namespace AssistantEngineer.Api.Services.Calculations.Composition;

public sealed class EngineeringWorkflowControllerActionService : IEngineeringWorkflowControllerActionService
{
    private readonly IEngineeringWorkflowStateBuilder _stateBuilder;
    private readonly IEngineeringWorkflowDiagnosticsService _workflowDiagnostics;
    private readonly IEngineeringWorkflowTracePreviewService _tracePreviewService;
    private readonly IEngineeringWorkflowReportPreviewService _reportPreviewService;
    private readonly IEngineeringReportJsonExporter _reportJsonExporter;
    private readonly IEngineeringReportMarkdownExporter _reportMarkdownExporter;
    private readonly IEngineeringCalculationScenarioRunner _scenarioRunner;
    private readonly IEngineeringWorkflowPersistenceService _workflowPersistence;

    public EngineeringWorkflowControllerActionService(
        IEngineeringWorkflowStateBuilder stateBuilder,
        IEngineeringWorkflowDiagnosticsService workflowDiagnostics,
        IEngineeringWorkflowTracePreviewService tracePreviewService,
        IEngineeringWorkflowReportPreviewService reportPreviewService,
        IEngineeringReportJsonExporter reportJsonExporter,
        IEngineeringReportMarkdownExporter reportMarkdownExporter,
        IEngineeringCalculationScenarioRunner scenarioRunner,
        IEngineeringWorkflowPersistenceService workflowPersistence)
    {
        _stateBuilder = stateBuilder;
        _workflowDiagnostics = workflowDiagnostics;
        _tracePreviewService = tracePreviewService;
        _reportPreviewService = reportPreviewService;
        _reportJsonExporter = reportJsonExporter;
        _reportMarkdownExporter = reportMarkdownExporter;
        _scenarioRunner = scenarioRunner;
        _workflowPersistence = workflowPersistence;
    }

    public async Task<EngineeringWorkflowValidationResponseDto> ValidateAsync(
        EngineeringWorkflowValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        var diagnostics = _workflowDiagnostics.ValidateState(request.State);
        var steps = _workflowDiagnostics.BuildStepStatuses(request.State, diagnostics);
        var stateToPersist = request.State with
        {
            Diagnostics = diagnostics,
            Steps = steps
        };

        await _workflowPersistence.SaveWorkflowStateAsync(stateToPersist, diagnostics, cancellationToken);

        return new EngineeringWorkflowValidationResponseDto(
            IsValid: diagnostics.All(diagnostic => !_workflowDiagnostics.IsErrorSeverity(diagnostic.Severity)),
            Diagnostics: diagnostics,
            Steps: steps);
    }

    public async Task<EngineeringWorkflowCalculationPreparationResponseDto> PrepareCalculationAsync(
        EngineeringWorkflowCalculationPreparationRequestDto request,
        CancellationToken cancellationToken)
    {
        var scenarioRequest = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: $"wf-prep-{request.State.ProjectId}-{request.State.BuildingId?.ToString() ?? "none"}",
            ProjectId: request.State.ProjectId,
            BuildingId: request.State.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.PrepareOnly,
            State: request.State,
            RequestedModules: request.State.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: false,
            IncludeReport: false,
            ReportFormats: ["Json"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");

        var scenarioResult = await _scenarioRunner.RunAsync(scenarioRequest, cancellationToken);
        var persistedScenario = await _workflowPersistence.SavePreparedScenarioAsync(
            scenarioRequest,
            scenarioResult,
            cancellationToken);

        var preview = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectId"] = request.State.ProjectId.ToString(),
            ["projectName"] = request.State.ProjectName,
            ["buildingId"] = request.State.BuildingId?.ToString() ?? "n/a",
            ["currentStep"] = request.State.CurrentStep,
            ["zonesCount"] = request.State.Zones.Count.ToString(),
            ["boundariesCount"] = request.State.Boundaries.Count.ToString(),
            ["diagnosticsCount"] = scenarioResult.ValidationDiagnostics.Count.ToString(),
            ["availableModulesCount"] = request.State.AvailableModules.Count.ToString(),
            ["scenarioStatus"] = scenarioResult.Status.ToString(),
            ["scenarioId"] = persistedScenario.ScenarioId
        };

        var status = scenarioResult.Status is EngineeringCalculationExecutionStatus.FailedValidation or EngineeringCalculationExecutionStatus.FailedExecution
            ? "blocked"
            : "prepared";

        var providerInfo = _workflowPersistence.GetProviderInfo();
        var metadata = scenarioResult.Metadata
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        metadata["persistence"] = providerInfo.ProviderLabel;
        metadata["persistenceProvider"] = providerInfo.Provider.ToString();
        metadata["durablePersistenceEnabled"] = providerInfo.DurableEnabled ? "true" : "false";

        return new EngineeringWorkflowCalculationPreparationResponseDto(
            RequestId: persistedScenario.ScenarioId,
            Status: status,
            Executed: false,
            RequestPreview: preview,
            Assumptions: scenarioResult.Assumptions,
            Diagnostics: scenarioResult.ValidationDiagnostics,
            Metadata: metadata);
    }

    public async Task<EngineeringWorkflowStateDto> BuildOrLoadWorkflowStateAsync(
        int projectId,
        int? buildingId,
        CancellationToken cancellationToken)
    {
        var persistedState = await _workflowPersistence.GetLatestWorkflowStateAsync(
            projectId,
            buildingId,
            cancellationToken);

        if (persistedState is not null)
        {
            return persistedState;
        }

        EngineeringWorkflowStateDto state;

        try
        {
            state = await _stateBuilder.BuildWorkflowStateAsync(projectId, buildingId, cancellationToken);
            state = _workflowDiagnostics.AddMissingPersistedStateDiagnostic(state, MapWorkflowPersistenceProviderInfo(_workflowPersistence.GetProviderInfo()));
            await _workflowPersistence.SaveWorkflowStateAsync(state, state.Diagnostics, cancellationToken);
        }
        catch (Exception exception)
        {
            state = _stateBuilder.BuildInfrastructureFallbackState(
                projectId,
                buildingId,
                $"Workflow persistence source is unavailable: {exception.Message}");
        }

        return state;
    }

    public EngineeringWorkflowTracePreviewResponseDto BuildTracePreview(EngineeringWorkflowTracePreviewRequestDto request)
    {
        var detailLevel = _tracePreviewService.ParseDetailLevel(request.DetailLevel);
        var diagnostics = _workflowDiagnostics.ValidateState(request.State);
        var trace = _tracePreviewService.BuildTraceDocument(request.State, detailLevel, diagnostics);
        var summary = _tracePreviewService.BuildTraceSummary(trace, request.DetailLevel);

        return new EngineeringWorkflowTracePreviewResponseDto(
            TraceDocument: trace,
            TraceSummary: summary,
            Diagnostics: diagnostics);
    }

    public EngineeringWorkflowReportResponseDto BuildReport(EngineeringWorkflowReportRequestDto request)
    {
        var diagnostics = _workflowDiagnostics.ValidateState(request.State);
        var reportDocument = _reportPreviewService.BuildReportDocument(request, diagnostics);
        var preview = _reportPreviewService.BuildReportPreview(reportDocument);

        return new EngineeringWorkflowReportResponseDto(
            ReportDocument: reportDocument,
            Preview: preview,
            Diagnostics: diagnostics);
    }

    public EngineeringWorkflowReportExportResponseDto BuildJsonExport(EngineeringWorkflowReportExportRequestDto request)
    {
        var diagnostics = _workflowDiagnostics.ValidateState(request.Request.State);
        var reportDocument = _reportPreviewService.BuildReportDocument(request.Request, diagnostics);
        var content = _reportJsonExporter.Export(reportDocument, indented: true);

        return new EngineeringWorkflowReportExportResponseDto(
            Format: "Json",
            Content: content,
            SchemaVersion: reportDocument.SchemaVersion,
            ReportId: reportDocument.ReportId,
            Diagnostics: diagnostics);
    }

    public EngineeringWorkflowReportExportResponseDto BuildMarkdownExport(EngineeringWorkflowReportExportRequestDto request)
    {
        var diagnostics = _workflowDiagnostics.ValidateState(request.Request.State);
        var reportDocument = _reportPreviewService.BuildReportDocument(request.Request, diagnostics);
        var content = _reportMarkdownExporter.Export(reportDocument);

        return new EngineeringWorkflowReportExportResponseDto(
            Format: "Markdown",
            Content: content,
            SchemaVersion: reportDocument.SchemaVersion,
            ReportId: reportDocument.ReportId,
            Diagnostics: diagnostics);
    }

    private static AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProviderInfo MapWorkflowPersistenceProviderInfo(
        EngineeringWorkflowPersistenceProviderInfo providerInfo)
    {
        var provider = providerInfo.Provider switch
        {
            EngineeringWorkflowPersistenceProvider.SQLite => AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProvider.SQLite,
            EngineeringWorkflowPersistenceProvider.None => AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProvider.None,
            _ => AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProvider.InMemory
        };

        return new AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProviderInfo(
            Provider: provider,
            DurableEnabled: providerInfo.DurableEnabled,
            ProviderLabel: providerInfo.ProviderLabel);
    }
}
