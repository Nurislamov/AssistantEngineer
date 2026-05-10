using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/engineering-workflow")]
public sealed class EngineeringWorkflowController : ControllerBase
{
    private static readonly string[] WorkflowSteps =
    [
        "Project",
        "Building",
        "Zones",
        "Envelope",
        "WeatherSolar",
        "Ventilation",
        "Ground",
        "DomesticHotWater",
        "SystemEnergy",
        "Validation",
        "CalculationTrace",
        "Reports",
        "Review"
    ];

    private static readonly string[] AvailableModules =
    [
        "Weather",
        "Solar",
        "ThermalTopology",
        "Iso52016",
        "MultiZone",
        "Ventilation",
        "Ground",
        "DomesticHotWater",
        "SystemEnergy",
        "Validation",
        "Reporting"
    ];

    private const int DefaultWeatherYear = 2020;

    private readonly IBuildingsFacade _buildings;
    private readonly IEngineeringCoreStatusFacade _engineeringCoreStatus;
    private readonly ICalculationTraceBuilder _traceBuilder;
    private readonly ICalculationTraceSanitizer _traceSanitizer;
    private readonly IEngineeringReportBuilder _reportBuilder;
    private readonly IEngineeringReportJsonExporter _reportJsonExporter;
    private readonly IEngineeringReportMarkdownExporter _reportMarkdownExporter;
    private readonly IEngineeringCalculationScenarioRunner _scenarioRunner;
    private readonly IEngineeringWorkflowPersistenceService _workflowPersistence;

    public EngineeringWorkflowController(
        IBuildingsFacade buildings,
        IEngineeringCoreStatusFacade engineeringCoreStatus,
        ICalculationTraceBuilder traceBuilder,
        ICalculationTraceSanitizer traceSanitizer,
        IEngineeringReportBuilder reportBuilder,
        IEngineeringReportJsonExporter reportJsonExporter,
        IEngineeringReportMarkdownExporter reportMarkdownExporter,
        IEngineeringCalculationScenarioRunner scenarioRunner,
        IEngineeringWorkflowPersistenceService workflowPersistence)
    {
        _buildings = buildings;
        _engineeringCoreStatus = engineeringCoreStatus;
        _traceBuilder = traceBuilder;
        _traceSanitizer = traceSanitizer;
        _reportBuilder = reportBuilder;
        _reportJsonExporter = reportJsonExporter;
        _reportMarkdownExporter = reportMarkdownExporter;
        _scenarioRunner = scenarioRunner;
        _workflowPersistence = workflowPersistence;
    }

    [HttpGet("{projectId:int}/state")]
    public async Task<ActionResult<EngineeringWorkflowStateDto>> GetWorkflowState(
        int projectId,
        [FromQuery] int? buildingId,
        CancellationToken cancellationToken)
    {
        var persistedState = await _workflowPersistence.GetLatestWorkflowStateAsync(
            projectId,
            buildingId,
            cancellationToken);

        if (persistedState is not null)
        {
            return Ok(persistedState);
        }

        EngineeringWorkflowStateDto state;

        try
        {
            state = await BuildWorkflowStateAsync(projectId, buildingId, cancellationToken);
            state = AddMissingPersistedStateDiagnostic(state);
            await _workflowPersistence.SaveWorkflowStateAsync(state, state.Diagnostics, cancellationToken);
        }
        catch (Exception exception)
        {
            state = BuildInfrastructureFallbackState(
                projectId,
                buildingId,
                $"Workflow persistence source is unavailable: {exception.Message}");
        }

        return Ok(state);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<EngineeringWorkflowValidationResponseDto>> Validate(
        [FromBody] EngineeringWorkflowValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        var diagnostics = ValidateState(request.State);
        var steps = BuildStepStatuses(request.State, diagnostics);
        var stateToPersist = request.State with
        {
            Diagnostics = diagnostics,
            Steps = steps
        };

        await _workflowPersistence.SaveWorkflowStateAsync(stateToPersist, diagnostics, cancellationToken);

        return Ok(new EngineeringWorkflowValidationResponseDto(
            IsValid: diagnostics.All(diagnostic => !IsErrorSeverity(diagnostic.Severity)),
            Diagnostics: diagnostics,
            Steps: steps));
    }

    [HttpPost("prepare-calculation")]
    public async Task<ActionResult<EngineeringWorkflowCalculationPreparationResponseDto>> PrepareCalculation(
        [FromBody] EngineeringWorkflowCalculationPreparationRequestDto request,
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

        var response = new EngineeringWorkflowCalculationPreparationResponseDto(
            RequestId: persistedScenario.ScenarioId,
            Status: status,
            Executed: false,
            RequestPreview: preview,
            Assumptions: scenarioResult.Assumptions,
            Diagnostics: scenarioResult.ValidationDiagnostics,
            Metadata: scenarioResult.Metadata);

        return Ok(response);
    }

    [HttpPost("run-calculation")]
    public async Task<ActionResult<EngineeringCalculationScenarioResultDto>> RunCalculation(
        [FromBody] EngineeringCalculationScenarioRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _scenarioRunner.RunAsync(request, cancellationToken);
        await _workflowPersistence.SaveRunScenarioAsync(request, result, cancellationToken);

        return Ok(result);
    }

    [HttpGet("scenarios/{scenarioId}")]
    public async Task<ActionResult<EngineeringCalculationScenarioRecordDto>> GetScenarioResult(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var scenario = await _workflowPersistence.GetScenarioAsync(scenarioId, cancellationToken);
        if (scenario is null)
        {
            return NotFound(new
            {
                scenarioId,
                code = "WORKFLOW_SCENARIO_NOT_FOUND",
                message = "Scenario record was not found in workflow persistence foundation store."
            });
        }

        return Ok(scenario);
    }

    [HttpGet("{projectId:int}/scenarios")]
    public async Task<ActionResult<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>> GetProjectScenarios(
        int projectId,
        CancellationToken cancellationToken)
    {
        var scenarios = await _workflowPersistence.ListProjectScenariosAsync(projectId, cancellationToken);
        return Ok(scenarios);
    }

    [HttpGet("scenarios/{scenarioId}/artifacts")]
    public async Task<ActionResult<IReadOnlyList<EngineeringCalculationArtifactRecordDto>>> GetScenarioArtifacts(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var artifacts = await _workflowPersistence.ListScenarioArtifactsAsync(scenarioId, cancellationToken);
        return Ok(artifacts);
    }

    [HttpGet("scenarios/{scenarioId}/artifacts/{artifactKind}")]
    public async Task<ActionResult<EngineeringCalculationArtifactRecordDto>> GetScenarioArtifactByKind(
        string scenarioId,
        string artifactKind,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EngineeringCalculationArtifactKind>(artifactKind, true, out var parsedKind))
        {
            return BadRequest(new
            {
                scenarioId,
                artifactKind,
                code = "WORKFLOW_ARTIFACT_KIND_INVALID",
                message = "Artifact kind is invalid for workflow persistence endpoint."
            });
        }

        var artifact = await _workflowPersistence.GetScenarioArtifactAsync(
            scenarioId,
            parsedKind,
            cancellationToken);

        if (artifact is null)
        {
            return NotFound(new
            {
                scenarioId,
                artifactKind = parsedKind.ToString(),
                code = "WORKFLOW_ARTIFACT_NOT_FOUND",
                message = "Scenario artifact was not found in workflow persistence foundation store."
            });
        }

        return Ok(artifact);
    }

    [HttpPost("trace-preview")]
    public ActionResult<EngineeringWorkflowTracePreviewResponseDto> TracePreview(
        [FromBody] EngineeringWorkflowTracePreviewRequestDto request)
    {
        var detailLevel = ParseTraceDetailLevel(request.DetailLevel);
        var diagnostics = ValidateState(request.State);

        var trace = BuildTraceDocument(request.State, detailLevel, diagnostics);
        var summary = BuildTraceSummary(trace, request.DetailLevel);

        return Ok(new EngineeringWorkflowTracePreviewResponseDto(
            TraceDocument: trace,
            TraceSummary: summary,
            Diagnostics: diagnostics));
    }

    [HttpPost("report")]
    public ActionResult<EngineeringWorkflowReportResponseDto> GenerateReport(
        [FromBody] EngineeringWorkflowReportRequestDto request)
    {
        var diagnostics = ValidateState(request.State);
        var reportDocument = BuildReportDocument(request, diagnostics);
        var preview = BuildReportPreview(reportDocument);

        return Ok(new EngineeringWorkflowReportResponseDto(
            ReportDocument: reportDocument,
            Preview: preview,
            Diagnostics: diagnostics));
    }

    [HttpPost("report/export/json")]
    public ActionResult<EngineeringWorkflowReportExportResponseDto> ExportReportJson(
        [FromBody] EngineeringWorkflowReportExportRequestDto request)
    {
        var diagnostics = ValidateState(request.Request.State);
        var reportDocument = BuildReportDocument(request.Request, diagnostics);
        var content = _reportJsonExporter.Export(reportDocument, indented: true);

        return Ok(new EngineeringWorkflowReportExportResponseDto(
            Format: "Json",
            Content: content,
            SchemaVersion: reportDocument.SchemaVersion,
            ReportId: reportDocument.ReportId,
            Diagnostics: diagnostics));
    }

    [HttpPost("report/export/markdown")]
    public ActionResult<EngineeringWorkflowReportExportResponseDto> ExportReportMarkdown(
        [FromBody] EngineeringWorkflowReportExportRequestDto request)
    {
        var diagnostics = ValidateState(request.Request.State);
        var reportDocument = BuildReportDocument(request.Request, diagnostics);
        var content = _reportMarkdownExporter.Export(reportDocument);

        return Ok(new EngineeringWorkflowReportExportResponseDto(
            Format: "Markdown",
            Content: content,
            SchemaVersion: reportDocument.SchemaVersion,
            ReportId: reportDocument.ReportId,
            Diagnostics: diagnostics));
    }

    private async Task<EngineeringWorkflowStateDto> BuildWorkflowStateAsync(
        int projectId,
        int? buildingId,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<EngineeringWorkflowDiagnosticDto>();
        var assumptions = new List<string>
        {
            "Workflow API state is orchestration-focused and deterministic.",
            "Endpoint aggregates existing modules and does not execute calculation physics directly.",
            "Partial data returns diagnostics rather than fake successful completion."
        };

        var projectResult = await _buildings.GetProjectByIdAsync(projectId, cancellationToken);
        var projectName = projectResult.IsSuccess ? projectResult.Value.Name : $"Project #{projectId}";

        if (projectResult.IsFailure)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_PROJECT_NOT_FOUND",
                Message: projectResult.Error,
                SourceStep: "Project",
                SuggestedCorrection: "Select an existing project or create a new one before workflow run."));
        }

        var buildingsResult = await _buildings.GetBuildingsByProjectAsync(projectId, cancellationToken);
        var buildings = buildingsResult.IsSuccess ? buildingsResult.Value : [];

        if (buildingsResult.IsFailure)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_BUILDINGS_LIST_UNAVAILABLE",
                Message: buildingsResult.Error,
                SourceStep: "Building",
                SuggestedCorrection: "Ensure project/building persistence is available."));
        }

        var selectedBuildingId = buildingId ?? buildings.FirstOrDefault()?.Id;

        if (!selectedBuildingId.HasValue)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_BUILDING_NOT_SELECTED",
                Message: "No building is available for selected project.",
                SourceStep: "Building",
                SuggestedCorrection: "Create at least one building in the selected project."));

            return new EngineeringWorkflowStateDto(
                ProjectId: projectId,
                ProjectName: projectName,
                BuildingId: null,
                CurrentStep: "Building",
                Steps: BuildStepStatusesForMissingBuilding(diagnostics),
                AvailableModules: AvailableModules,
                BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                    BuildingName: null,
                    LocationText: "Building is not selected",
                    FloorAreaM2: null,
                    VolumeM3: null,
                    NumberOfZones: null,
                    Notes: "Foundation workflow state."),
                Zones: [],
                Boundaries: [],
                WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto(
                    WeatherSourceStatus: "Unavailable",
                    LocationTimezoneSummary: "Building context is missing",
                    SolarChainReadinessSummary: "Pending building selection"),
                VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(
                    OpeningCount: 0,
                    ControlModeSummary: "Unavailable",
                    AirflowSummary: "No building selected",
                    Warnings: []),
                GroundSettings: new EngineeringWorkflowGroundSettingsDto(
                    GroundBoundaryCount: 0,
                    GroundProfileMode: "Unavailable",
                    SummaryStatus: "incomplete"),
                DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto(
                    DemandBasis: "Not available",
                    UsefulDemandSummary: "No building selected",
                    LossesSummary: "No building selected",
                    OwnershipPolicy: "No data"),
                SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto(
                    UsesSummary: "No data",
                    CarriersSummary: "No data",
                    FinalPrimaryCarbonSummary: "No data"),
                Diagnostics: SortAndDistinctDiagnostics(diagnostics).ToArray(),
                Assumptions: assumptions,
                Links:
                [
                    $"/api/v1/projects/{projectId}/buildings",
                    "/api/v1/engineering-workflow/validate"
                ],
                CalculationTraceSummary: null,
                ReportSummary: null,
                Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["mode"] = "api",
                    ["stage"] = "foundation",
                    ["buildingSelection"] = "missing"
                });
        }

        var buildingResult = await _buildings.GetBuildingByIdAsync(selectedBuildingId.Value, cancellationToken);
        if (buildingResult.IsFailure)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_BUILDING_UNAVAILABLE",
                Message: buildingResult.Error,
                SourceStep: "Building",
                SuggestedCorrection: "Re-select a building and retry workflow state request."));

            return await BuildWorkflowStateAsync(projectId, null, cancellationToken);
        }

        var building = buildingResult.Value;

        var roomsResult = await _buildings.GetRoomsByBuildingAsync(selectedBuildingId.Value, cancellationToken);
        var zonesResult = await _buildings.GetThermalZonesByBuildingAsync(selectedBuildingId.Value, cancellationToken);
        var readinessResult = await _buildings.CheckBuildingReadinessAsync(
            selectedBuildingId.Value,
            DefaultWeatherYear,
            cancellationToken);
        var validationResult = await _buildings.ValidateBuildingModelAsync(
            selectedBuildingId.Value,
            DefaultWeatherYear,
            cancellationToken);
        var coreStatusResult = _engineeringCoreStatus.GetEngineeringCoreV1Status();

        if (roomsResult.IsFailure)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto("warning", "WORKFLOW_ROOMS_UNAVAILABLE", roomsResult.Error, "Building"));
        }

        if (zonesResult.IsFailure)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto("warning", "WORKFLOW_ZONES_UNAVAILABLE", zonesResult.Error, "Zones"));
        }

        if (readinessResult.IsFailure)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto("warning", "WORKFLOW_READINESS_UNAVAILABLE", readinessResult.Error, "WeatherSolar"));
        }

        if (validationResult.IsFailure)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto("warning", "WORKFLOW_VALIDATION_UNAVAILABLE", validationResult.Error, "Validation"));
        }

        if (coreStatusResult.IsFailure)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto("warning", "WORKFLOW_CORE_STATUS_UNAVAILABLE", coreStatusResult.Error, "WeatherSolar"));
        }

        var rooms = roomsResult.IsSuccess ? roomsResult.Value : [];
        var zones = zonesResult.IsSuccess ? zonesResult.Value : [];

        var walls = new List<WallResponse>();
        var windows = new List<WindowResponse>();
        var ventilationConfiguredRoomCount = 0;
        var groundConfiguredRoomCount = 0;

        foreach (var room in rooms)
        {
            var roomWalls = await _buildings.GetRoomWallsAsync(room.Id, cancellationToken);
            if (roomWalls.IsSuccess)
            {
                walls.AddRange(roomWalls.Value);
            }

            var roomWindows = await _buildings.GetRoomWindowsAsync(room.Id, cancellationToken);
            if (roomWindows.IsSuccess)
            {
                windows.AddRange(roomWindows.Value);
            }

            var roomVentilation = await _buildings.GetRoomVentilationParametersAsync(room.Id, cancellationToken);
            if (roomVentilation.IsSuccess)
            {
                ventilationConfiguredRoomCount++;
            }

            var roomGround = await _buildings.GetRoomGroundContactAsync(room.Id, cancellationToken);
            if (roomGround.IsSuccess)
            {
                groundConfiguredRoomCount++;
            }
        }

        var boundaryDtos = walls
            .OrderBy(wall => wall.RoomId)
            .ThenBy(wall => wall.Id)
            .Select(wall => new EngineeringWorkflowBoundaryDto(
                BoundaryId: wall.Id.ToString(),
                ZoneOrRoomName: rooms.FirstOrDefault(room => room.Id == wall.RoomId)?.Name ?? $"Room {wall.RoomId}",
                ExposureKind: wall.BoundaryType.ToString(),
                AreaM2: wall.AreaM2,
                UValue: wall.UValue,
                AdjacentZoneReference: wall.AdjacentRoomId?.ToString(),
                Indicator: MapBoundaryIndicator(wall.BoundaryType),
                ValidationStatus: wall.BoundaryType == WallBoundaryTypeDto.AdjacentUnconditioned ? "warnings" : "valid"))
            .ToArray();

        var zoneDtos = zones
            .OrderBy(zone => zone.Name, StringComparer.Ordinal)
            .Select(zone =>
            {
                var zoneRoomIds = zone.Rooms.Select(room => room.Id).ToHashSet();
                var zoneArea = rooms.Where(room => zoneRoomIds.Contains(room.Id)).Sum(room => room.AreaM2);
                var zoneVolume = rooms.Where(room => zoneRoomIds.Contains(room.Id)).Sum(room => room.VolumeM3);

                return new EngineeringWorkflowZoneDto(
                    ZoneId: zone.Id.ToString(),
                    Name: zone.Name,
                    ZoneKind: zone.Rooms.Count > 1 ? "Multi-room" : "Single-room",
                    FloorAreaM2: zoneArea,
                    AirVolumeM3: zoneVolume,
                    Status: zone.Rooms.Count == 0 ? "warnings" : "valid");
            })
            .ToArray();

        if (!zoneDtos.Any())
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_ZONES_MISSING",
                Message: "No thermal zones configured for selected building.",
                SourceStep: "Zones",
                SuggestedCorrection: "Configure at least one thermal zone before full workflow execution."));
        }

        if (!boundaryDtos.Any())
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_BOUNDARIES_MISSING",
                Message: "No envelope boundaries configured for selected building.",
                SourceStep: "Envelope",
                SuggestedCorrection: "Add wall boundaries to support envelope-dependent calculations."));
        }

        if (groundConfiguredRoomCount == 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_GROUND_BOUNDARY_MISSING",
                Message: "Ground boundary settings are not configured for any room.",
                SourceStep: "Ground",
                SuggestedCorrection: "Configure ground contact parameters for at least one room."));
        }

        if (readinessResult.IsSuccess)
        {
            diagnostics.AddRange(readinessResult.Value.Issues
                .Select(issue => new EngineeringWorkflowDiagnosticDto(
                    Severity: NormalizeSeverity(issue.Severity),
                    Code: "WORKFLOW_READINESS_ISSUE",
                    Message: issue.Message,
                    SourceStep: "WeatherSolar",
                    SourceModule: "Buildings",
                    TargetField: issue.Location)));
        }

        if (validationResult.IsSuccess)
        {
            diagnostics.AddRange(validationResult.Value.Issues
                .Select(issue => new EngineeringWorkflowDiagnosticDto(
                    Severity: NormalizeSeverity(issue.Severity),
                    Code: issue.Code,
                    Message: issue.Message,
                    SourceStep: "Validation",
                    SourceModule: "Buildings",
                    TargetField: issue.Location)));
        }

        diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
            Severity: "assumption",
            Code: "WORKFLOW_DHW_SUMMARY_FOUNDATION",
            Message: "DHW workflow summary is foundation-level unless dedicated building-level runner is wired.",
            SourceStep: "DomesticHotWater"));

        diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
            Severity: "assumption",
            Code: "WORKFLOW_SYSTEM_ENERGY_SUMMARY_FOUNDATION",
            Message: "System energy workflow summary is foundation-level unless dedicated runner is wired.",
            SourceStep: "SystemEnergy"));

        var diagnosticsSorted = SortAndDistinctDiagnostics(diagnostics).ToArray();
        var buildingArea = rooms.Sum(room => room.AreaM2);
        var buildingVolume = rooms.Sum(room => room.VolumeM3);

        var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["mode"] = "api",
            ["stage"] = "foundation",
            ["weatherYear"] = DefaultWeatherYear.ToString(),
            ["buildingSelection"] = "resolved"
        };

        var provisionalState = new EngineeringWorkflowStateDto(
            ProjectId: projectId,
            ProjectName: projectName,
            BuildingId: building.Id,
            CurrentStep: "Project",
            Steps: [],
            AvailableModules: AvailableModules,
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: building.Name,
                LocationText: building.ClimateZoneName ?? "Climate zone not assigned",
                FloorAreaM2: buildingArea,
                VolumeM3: buildingVolume,
                NumberOfZones: zones.Count,
                Notes: "Internal engineering workflow API foundation state."),
            Zones: zoneDtos,
            Boundaries: boundaryDtos,
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto("n/a", "n/a", "n/a"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(0, "n/a", "n/a", []),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(groundConfiguredRoomCount, "n/a", groundConfiguredRoomCount > 0 ? "valid" : "incomplete"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto("n/a", "n/a", "n/a", "n/a"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto("n/a", "n/a", "n/a"),
            Diagnostics: diagnosticsSorted,
            Assumptions: assumptions,
            Links: [],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: metadata);

        var traceSummary = BuildTraceSummary(
            BuildTraceDocument(provisionalState, CalculationTraceDetailLevel.Summary, diagnosticsSorted),
            "Summary");

        var reportPreview = new EngineeringWorkflowReportPreviewDto(
            ReportKind: "FullEngineeringCore",
            Title: "Full engineering workflow preview",
            Sections:
            [
                "Executive summary",
                "Input summary",
                "Assumptions",
                "Warnings",
                "Validation diagnostics",
                "Weather and solar",
                "Thermal zones",
                "Natural ventilation",
                "Ground boundaries",
                "Domestic hot water",
                "System energy",
                "Calculation trace appendix",
                "Limitations"
            ],
            WarningsCount: diagnosticsSorted.Count(diagnostic => diagnostic.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase)),
            DiagnosticsCount: diagnosticsSorted.Length,
            ExportFormatsAvailable: ["Json", "Markdown"],
            GeneratedTimestampUtc: DateTimeOffset.UtcNow,
            Limitations:
            [
                "Workflow API is foundation-level and may prepare/preview without executing full production scenario.",
                "Workflow API is not a compliance certificate.",
                "Reports summarize current internal engineering calculations only.",
                "Trace explains internal calculation chain only.",
                "No external validation evidence.",
                "No full standard compliance claim."
            ]);

        var finalState = provisionalState with
        {
            CurrentStep = SelectCurrentStep(zoneDtos, boundaryDtos, diagnosticsSorted),
            Steps = BuildStepStatuses(provisionalState, diagnosticsSorted),
            WeatherSolarSettings = new EngineeringWorkflowWeatherSolarSettingsDto(
                WeatherSourceStatus: readinessResult.IsSuccess && readinessResult.Value.IsReady ? "Ready" : "Requires fixes",
                LocationTimezoneSummary: $"Weather year {DefaultWeatherYear}, climate: {building.ClimateZoneName ?? "n/a"}",
                SolarChainReadinessSummary: coreStatusResult.IsSuccess && coreStatusResult.Value.Weather8760GatesClosed
                    ? "Solar and weather chain readiness gate is closed (ClosedV1)."
                    : "Solar/weather gate requires review."),
            VentilationSettings = new EngineeringWorkflowVentilationSettingsDto(
                OpeningCount: windows.Count,
                ControlModeSummary: ventilationConfiguredRoomCount > 0
                    ? "Ventilation parameters are configured for at least one room."
                    : "Ventilation parameters are not configured.",
                AirflowSummary: $"Configured rooms: {ventilationConfiguredRoomCount}/{rooms.Count}",
                Warnings: diagnosticsSorted
                    .Where(diagnostic => diagnostic.SourceStep == "Ventilation" &&
                                         diagnostic.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
                    .Select(diagnostic => diagnostic.Message)
                    .ToArray()),
            GroundSettings = new EngineeringWorkflowGroundSettingsDto(
                GroundBoundaryCount: groundConfiguredRoomCount,
                GroundProfileMode: groundConfiguredRoomCount > 0 ? "Room ground-contact profiles" : "Not configured",
                SummaryStatus: groundConfiguredRoomCount > 0 ? "valid" : "incomplete"),
            DomesticHotWaterSettings = new EngineeringWorkflowDomesticHotWaterSettingsDto(
                DemandBasis: "Foundation-level workflow summary",
                UsefulDemandSummary: "Use dedicated DHW endpoint for full scenario execution.",
                LossesSummary: "Losses and recovered gains require dedicated DHW runner wiring.",
                OwnershipPolicy: "No double-counting decisions remain in backend system energy modules."),
            SystemEnergySettings = new EngineeringWorkflowSystemEnergySettingsDto(
                UsesSummary: "Foundation summary only.",
                CarriersSummary: "Carrier split requires dedicated system energy endpoint wiring.",
                FinalPrimaryCarbonSummary: "Use report endpoint/export for deterministic preview output."),
            Links =
            [
                $"/api/v1/engineering-workflow/{projectId}/state?buildingId={building.Id}",
                "/api/v1/engineering-workflow/validate",
                "/api/v1/engineering-workflow/prepare-calculation",
                "/api/v1/engineering-workflow/trace-preview",
                "/api/v1/engineering-workflow/report",
                "/api/v1/engineering-workflow/report/export/json",
                "/api/v1/engineering-workflow/report/export/markdown"
            ],
            CalculationTraceSummary = traceSummary,
            ReportSummary = reportPreview
        };

        return finalState;
    }

    private EngineeringReportDocument BuildReportDocument(
        EngineeringWorkflowReportRequestDto request,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        var detailLevel = ParseReportDetailLevel(request.DetailLevel);
        var reportKind = ParseReportKind(request.ReportKind);
        var reportFormat = ParseReportFormat(request.RequestedFormat);
        var traceDetailLevel = request.DetailLevel.Equals("Detailed", StringComparison.OrdinalIgnoreCase)
            ? CalculationTraceDetailLevel.Detailed
            : request.DetailLevel.Equals("Summary", StringComparison.OrdinalIgnoreCase)
                ? CalculationTraceDetailLevel.Summary
                : CalculationTraceDetailLevel.Standard;

        var traceDocument = request.IncludeTraceAppendix
            ? BuildTraceDocument(request.State, traceDetailLevel, diagnostics)
            : null;

        var calculationDiagnostics = diagnostics
            .Where(diagnostic => !diagnostic.Severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
            .Select(diagnostic => new CalculationDiagnostic(
                Severity: ParseCalculationDiagnosticSeverity(diagnostic.Severity),
                Code: diagnostic.Code,
                Message: diagnostic.Message,
                Context: diagnostic.TargetField ?? diagnostic.SourceModule))
            .ToArray();

        var reportRequest = new EngineeringReportGenerationRequest(
            ReportKind: reportKind,
            RequestedFormat: reportFormat,
            ReportTitle: $"Engineering workflow report - {request.State.ProjectName}",
            ProjectId: request.State.ProjectId.ToString(),
            BuildingId: request.State.BuildingId?.ToString(),
            ValidationDiagnostics: calculationDiagnostics,
            CalculationTrace: traceDocument,
            DetailLevel: detailLevel,
            IncludeTraceAppendix: request.IncludeTraceAppendix,
            IncludeLimitations: request.IncludeLimitations,
            Assumptions: request.State.Assumptions,
            Warnings: diagnostics
                .Where(diagnostic => diagnostic.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
                .Select(diagnostic => diagnostic.Message)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            SourceCalculationIds: request.State.CalculationTraceSummary?.CalculationId is null
                ? []
                : [request.State.CalculationTraceSummary.CalculationId],
            Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["workflow.mode"] = "api",
                ["workflow.step"] = request.State.CurrentStep,
                ["workflow.stage"] = "foundation"
            });

        return _reportBuilder.Build(reportRequest);
    }

    private static EngineeringWorkflowReportPreviewDto BuildReportPreview(EngineeringReportDocument report)
    {
        return new EngineeringWorkflowReportPreviewDto(
            ReportKind: report.ReportKind.ToString(),
            Title: report.Title,
            Sections: report.Sections
                .OrderBy(section => section.Order)
                .Select(section => section.Title)
                .ToArray(),
            WarningsCount: report.Warnings.Count,
            DiagnosticsCount: report.Diagnostics.Count,
            ExportFormatsAvailable: ["Json", "Markdown"],
            GeneratedTimestampUtc: report.GeneratedTimestampUtc,
            Limitations: report.Sections
                .Where(section => section.SectionKind == EngineeringReportSectionKind.Limitations)
                .SelectMany(section => section.KeyValues.Select(value => value.Value?.ToString() ?? string.Empty))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }

    private CalculationTraceDocument BuildTraceDocument(
        EngineeringWorkflowStateDto state,
        CalculationTraceDetailLevel detailLevel,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        _traceBuilder.SetDetailLevel(detailLevel);
        _traceBuilder.Initialize(
            traceId: $"workflow-trace-{state.ProjectId}-{state.BuildingId?.ToString() ?? "none"}",
            calculationType: "EngineeringWorkflowPreview",
            rootModule: CalculationTraceModuleKind.Generic,
            calculationId: state.CalculationTraceSummary?.CalculationId ?? state.BuildingId?.ToString(),
            metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["workflow.mode"] = "api",
                ["workflow.step"] = state.CurrentStep
            });

        var projectStepId = _traceBuilder.AddStep(
            moduleKind: CalculationTraceModuleKind.Validation,
            stepName: "Project and building context",
            formulaOrConventionLabel: "Workflow foundation context aggregation");

        _traceBuilder.AddInputValue(projectStepId, new CalculationTraceValue(
            Key: "project_id",
            Label: "Project id",
            Value: state.ProjectId,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Input));

        _traceBuilder.AddInputValue(projectStepId, new CalculationTraceValue(
            Key: "building_id",
            Label: "Building id",
            Value: state.BuildingId,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Input));

        _traceBuilder.AddOutputValue(projectStepId, new CalculationTraceValue(
            Key: "zones_count",
            Label: "Zones count",
            Value: state.Zones.Count,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output));

        _traceBuilder.AddOutputValue(projectStepId, new CalculationTraceValue(
            Key: "boundaries_count",
            Label: "Boundary count",
            Value: state.Boundaries.Count,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output));

        var diagnosticsStepId = _traceBuilder.AddStep(
            moduleKind: CalculationTraceModuleKind.Validation,
            stepName: "Validation diagnostics aggregation",
            formulaOrConventionLabel: "Deterministic severity-sorted diagnostics merge");

        foreach (var diagnostic in diagnostics)
        {
            _traceBuilder.AddDiagnostic(diagnosticsStepId, new CalculationTraceDiagnostic(
                Severity: ParseTraceSeverity(diagnostic.Severity),
                Code: diagnostic.Code,
                Message: diagnostic.Message,
                ModuleKind: ParseTraceModuleKind(diagnostic.SourceStep),
                Context: diagnostic.TargetField,
                Source: diagnostic.SourceModule));
        }

        var reportStepId = _traceBuilder.AddStep(
            moduleKind: CalculationTraceModuleKind.Reporting,
            stepName: "Report preview readiness",
            formulaOrConventionLabel: "Workflow foundation report orchestration");

        _traceBuilder.AddOutputValue(reportStepId, new CalculationTraceValue(
            Key: "current_step",
            Label: "Current step",
            Value: state.CurrentStep,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output));

        _traceBuilder.AddOutputValue(reportStepId, new CalculationTraceValue(
            Key: "diagnostics_count",
            Label: "Diagnostics count",
            Value: diagnostics.Count,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output));

        foreach (var assumption in state.Assumptions)
        {
            _traceBuilder.AddDocumentAssumption(assumption);
        }

        foreach (var warning in diagnostics
                     .Where(diagnostic => diagnostic.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
                     .Select(diagnostic => diagnostic.Message)
                     .Distinct(StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal))
        {
            _traceBuilder.AddDocumentWarning(warning);
        }

        foreach (var diagnostic in diagnostics)
        {
            _traceBuilder.AddDocumentDiagnostic(new CalculationTraceDiagnostic(
                Severity: ParseTraceSeverity(diagnostic.Severity),
                Code: diagnostic.Code,
                Message: diagnostic.Message,
                ModuleKind: ParseTraceModuleKind(diagnostic.SourceStep),
                Context: diagnostic.TargetField,
                Source: diagnostic.SourceModule));
        }

        var trace = _traceBuilder.Build();
        return _traceSanitizer.Sanitize(trace, detailLevel);
    }

    private static EngineeringWorkflowTraceSummaryDto BuildTraceSummary(
        CalculationTraceDocument trace,
        string detailLevel)
    {
        var modules = trace.Steps
            .Select(step => step.ModuleKind.ToString())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var stepSummaries = trace.Steps
            .OrderBy(step => step.Sequence)
            .Select(step => new EngineeringWorkflowTraceStepSummaryDto(
                StepId: step.StepId,
                ModuleKind: step.ModuleKind.ToString(),
                StepName: step.StepName,
                Sequence: step.Sequence,
                Assumptions: step.Assumptions,
                Warnings: step.Warnings,
                DiagnosticsCount: step.Diagnostics.Count))
            .ToArray();

        return new EngineeringWorkflowTraceSummaryDto(
            TraceId: trace.TraceId,
            CalculationId: trace.CalculationId,
            DetailLevel: detailLevel,
            Modules: modules,
            Assumptions: trace.Assumptions,
            Warnings: trace.Warnings,
            Steps: stepSummaries);
    }

    private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> ValidateState(
        EngineeringWorkflowStateDto state)
    {
        var diagnostics = new List<EngineeringWorkflowDiagnosticDto>(state.Diagnostics);

        if (state.ProjectId <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "WORKFLOW_PROJECT_ID_INVALID",
                Message: "Project id must be greater than zero.",
                SourceStep: "Project",
                TargetField: "projectId"));
        }

        if (!state.BuildingId.HasValue || state.BuildingId <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_BUILDING_NOT_SELECTED",
                Message: "Building is not selected.",
                SourceStep: "Building",
                TargetField: "buildingId"));
        }

        if (state.Zones.Count == 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_ZONES_MISSING",
                Message: "No zones available in workflow state.",
                SourceStep: "Zones"));
        }

        if (state.Boundaries.Count == 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_BOUNDARIES_MISSING",
                Message: "No boundaries available in workflow state.",
                SourceStep: "Envelope"));
        }

        if (state.GroundSettings.GroundBoundaryCount <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_GROUND_BOUNDARY_MISSING",
                Message: "Ground settings are not configured.",
                SourceStep: "Ground"));
        }

        return SortAndDistinctDiagnostics(diagnostics).ToArray();
    }

    private static IReadOnlyList<EngineeringWorkflowStepDto> BuildStepStatuses(
        EngineeringWorkflowStateDto state,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        return WorkflowSteps
            .Select(step =>
            {
                var diagnosticsForStep = diagnostics.Where(diagnostic =>
                    diagnostic.SourceStep.Equals(step, StringComparison.OrdinalIgnoreCase)).ToArray();

                var hasErrors = diagnosticsForStep.Any(diagnostic => IsErrorSeverity(diagnostic.Severity));
                var hasWarnings = diagnosticsForStep.Any(diagnostic =>
                    diagnostic.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase));

                var status = "valid";
                if (step == "Project" && state.ProjectId <= 0)
                {
                    status = "incomplete";
                }
                else if (step == "Building" && (!state.BuildingId.HasValue || state.BuildingId <= 0))
                {
                    status = "incomplete";
                }
                else if (step == "Zones" && state.Zones.Count == 0)
                {
                    status = "incomplete";
                }
                else if (step == "Envelope" && state.Boundaries.Count == 0)
                {
                    status = "incomplete";
                }
                else if (step == "Ground" && state.GroundSettings.GroundBoundaryCount <= 0)
                {
                    status = "incomplete";
                }
                else if (step == "CalculationTrace" && state.CalculationTraceSummary is null)
                {
                    status = "incomplete";
                }
                else if (step == "Reports" && state.ReportSummary is null)
                {
                    status = "incomplete";
                }

                if (hasErrors)
                {
                    status = "errors";
                }
                else if (hasWarnings && !status.Equals("incomplete", StringComparison.OrdinalIgnoreCase))
                {
                    status = "warnings";
                }

                var isComplete = status is "valid" or "warnings" or "errors";
                return new EngineeringWorkflowStepDto(step, status, isComplete);
            })
            .ToArray();
    }

    private static IReadOnlyList<EngineeringWorkflowStepDto> BuildStepStatusesForMissingBuilding(
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        return BuildStepStatuses(new EngineeringWorkflowStateDto(
            ProjectId: 1,
            ProjectName: "n/a",
            BuildingId: null,
            CurrentStep: "Building",
            Steps: [],
            AvailableModules: AvailableModules,
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(null, null, null, null, null, null),
            Zones: [],
            Boundaries: [],
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto("n/a", "n/a", "n/a"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(0, "n/a", "n/a", []),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(0, "n/a", "incomplete"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto("n/a", "n/a", "n/a", "n/a"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto("n/a", "n/a", "n/a"),
            Diagnostics: diagnostics,
            Assumptions: [],
            Links: [],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new Dictionary<string, string>()), diagnostics);
    }

    private static EngineeringWorkflowStateDto BuildInfrastructureFallbackState(
        int projectId,
        int? buildingId,
        string errorMessage)
    {
        var diagnostics = SortAndDistinctDiagnostics(
        [
            new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_PERSISTENCE_UNAVAILABLE",
                Message: errorMessage,
                SourceStep: "Project",
                SuggestedCorrection: "Ensure workflow persistence dependencies are available and retry."),
            new EngineeringWorkflowDiagnosticDto(
                Severity: "assumption",
                Code: "WORKFLOW_FOUNDATION_FALLBACK_STATE",
                Message: "Deterministic workflow fallback state is returned because backend persistence is unavailable.",
                SourceStep: "Validation")
        ]);

        var state = new EngineeringWorkflowStateDto(
            ProjectId: projectId,
            ProjectName: $"Project #{projectId}",
            BuildingId: buildingId,
            CurrentStep: "Project",
            Steps: [],
            AvailableModules: AvailableModules,
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: null,
                LocationText: "Persistence unavailable",
                FloorAreaM2: null,
                VolumeM3: null,
                NumberOfZones: null,
                Notes: "Internal engineering workflow fallback state."),
            Zones: [],
            Boundaries: [],
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto(
                WeatherSourceStatus: "Unavailable",
                LocationTimezoneSummary: "Persistence unavailable",
                SolarChainReadinessSummary: "Pending persistence availability"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(
                OpeningCount: 0,
                ControlModeSummary: "Unavailable",
                AirflowSummary: "Persistence unavailable",
                Warnings: []),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(
                GroundBoundaryCount: 0,
                GroundProfileMode: "Unavailable",
                SummaryStatus: "incomplete"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto(
                DemandBasis: "Unavailable",
                UsefulDemandSummary: "Unavailable",
                LossesSummary: "Unavailable",
                OwnershipPolicy: "Unavailable"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto(
                UsesSummary: "Unavailable",
                CarriersSummary: "Unavailable",
                FinalPrimaryCarbonSummary: "Unavailable"),
            Diagnostics: diagnostics,
            Assumptions:
            [
                "Workflow API returns deterministic fallback state when persistence dependencies are unavailable.",
                "Fallback state is internal foundation behavior and does not execute engineering physics."
            ],
            Links:
            [
                "/api/v1/engineering-workflow/validate",
                "/api/v1/engineering-workflow/report"
            ],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["mode"] = "api",
                ["stage"] = "foundation",
                ["persistence"] = "unavailable",
                ["fallback"] = "deterministic"
            });

        var steps = BuildStepStatuses(state, diagnostics);
        return state with { Steps = steps };
    }

    private static EngineeringWorkflowStateDto AddMissingPersistedStateDiagnostic(
        EngineeringWorkflowStateDto state)
    {
        var diagnostics = SortAndDistinctDiagnostics(state.Diagnostics.Concat(
        [
            new EngineeringWorkflowDiagnosticDto(
                Severity: "info",
                Code: "WORKFLOW_STATE_NOT_PERSISTED_YET",
                Message: "No persisted workflow state existed for this project; deterministic foundation state was generated and persisted.",
                SourceStep: "Project",
                SuggestedCorrection: "Continue workflow edits and use validate/prepare/run endpoints to create scenario history.")
        ]));

        var metadata = state.Metadata
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        metadata["persistence"] = "in-memory-foundation";
        metadata["stateSource"] = "generated-and-persisted";

        var updated = state with
        {
            Diagnostics = diagnostics,
            Metadata = metadata
        };

        return updated with { Steps = BuildStepStatuses(updated, diagnostics) };
    }

    private static string SelectCurrentStep(
        IReadOnlyList<EngineeringWorkflowZoneDto> zones,
        IReadOnlyList<EngineeringWorkflowBoundaryDto> boundaries,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        if (!zones.Any())
        {
            return "Zones";
        }

        if (!boundaries.Any())
        {
            return "Envelope";
        }

        if (diagnostics.Any(diagnostic => IsErrorSeverity(diagnostic.Severity)))
        {
            return "Validation";
        }

        return "Review";
    }

    private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return diagnostics
            .OrderByDescending(diagnostic => SeverityRank(diagnostic.Severity))
            .ThenBy(diagnostic => diagnostic.SourceStep, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .Where(diagnostic => seen.Add(
                $"{diagnostic.SourceStep}|{diagnostic.Code}|{diagnostic.Message}|{diagnostic.TargetField}"))
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

    private static string MapBoundaryIndicator(WallBoundaryTypeDto boundaryType)
    {
        return boundaryType switch
        {
            WallBoundaryTypeDto.External => "exterior",
            WallBoundaryTypeDto.Ground => "ground",
            WallBoundaryTypeDto.Adiabatic => "adiabatic",
            _ => "adjacent"
        };
    }

    private static string NormalizeSeverity(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return "info";
        }

        var normalized = source.Trim();
        if (normalized.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return "error";
        }

        if (normalized.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return "warning";
        }

        if (normalized.Equals("assumption", StringComparison.OrdinalIgnoreCase))
        {
            return "assumption";
        }

        return "info";
    }

    private static string NormalizeSeverity(BuildingCalculationReadinessSeverity severity)
    {
        return severity switch
        {
            BuildingCalculationReadinessSeverity.Error => "error",
            BuildingCalculationReadinessSeverity.Warning => "warning",
            _ => "info"
        };
    }

    private static bool IsErrorSeverity(string severity) =>
        severity.Equals("error", StringComparison.OrdinalIgnoreCase);

    private static CalculationTraceDetailLevel ParseTraceDetailLevel(string? detailLevel)
    {
        if (!string.IsNullOrWhiteSpace(detailLevel) &&
            Enum.TryParse<CalculationTraceDetailLevel>(detailLevel, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return CalculationTraceDetailLevel.Standard;
    }

    private static EngineeringReportDetailLevel ParseReportDetailLevel(string? detailLevel)
    {
        if (!string.IsNullOrWhiteSpace(detailLevel) &&
            Enum.TryParse<EngineeringReportDetailLevel>(detailLevel, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return EngineeringReportDetailLevel.Standard;
    }

    private static EngineeringReportKind ParseReportKind(string? reportKind)
    {
        if (!string.IsNullOrWhiteSpace(reportKind) &&
            Enum.TryParse<EngineeringReportKind>(reportKind, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return EngineeringReportKind.FullEngineeringCore;
    }

    private static EngineeringReportFormat ParseReportFormat(string? format)
    {
        if (!string.IsNullOrWhiteSpace(format) &&
            Enum.TryParse<EngineeringReportFormat>(format, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return EngineeringReportFormat.Json;
    }

    private static CalculationDiagnosticSeverity ParseCalculationDiagnosticSeverity(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationDiagnosticSeverity.Error;
        }

        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationDiagnosticSeverity.Warning;
        }

        return CalculationDiagnosticSeverity.Info;
    }

    private static CalculationTraceSeverity ParseTraceSeverity(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationTraceSeverity.Error;
        }

        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationTraceSeverity.Warning;
        }

        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationTraceSeverity.Assumption;
        }

        if (severity.Equals("debug", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationTraceSeverity.Debug;
        }

        return CalculationTraceSeverity.Info;
    }

    private static CalculationTraceModuleKind ParseTraceModuleKind(string sourceStep)
    {
        return sourceStep switch
        {
            "WeatherSolar" => CalculationTraceModuleKind.Weather,
            "Zones" => CalculationTraceModuleKind.ThermalTopology,
            "Envelope" => CalculationTraceModuleKind.ThermalTopology,
            "Ventilation" => CalculationTraceModuleKind.Ventilation,
            "Ground" => CalculationTraceModuleKind.Ground,
            "DomesticHotWater" => CalculationTraceModuleKind.DomesticHotWater,
            "SystemEnergy" => CalculationTraceModuleKind.SystemEnergy,
            "Reports" => CalculationTraceModuleKind.Reporting,
            "Validation" => CalculationTraceModuleKind.Validation,
            _ => CalculationTraceModuleKind.Generic
        };
    }
}
