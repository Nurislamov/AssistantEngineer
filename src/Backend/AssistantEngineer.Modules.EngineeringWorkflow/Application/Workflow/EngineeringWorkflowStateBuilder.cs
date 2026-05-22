using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public sealed class EngineeringWorkflowStateBuilder : IEngineeringWorkflowStateBuilder
{
    private readonly IEngineeringWorkflowInputSnapshotBuilder _inputSnapshotBuilder;
    private readonly IEngineeringWorkflowDiagnosticsService _workflowDiagnostics;
    private readonly IEngineeringWorkflowTracePreviewService _tracePreviewService;

    public EngineeringWorkflowStateBuilder(
        IEngineeringWorkflowInputSnapshotBuilder inputSnapshotBuilder,
        IEngineeringWorkflowDiagnosticsService workflowDiagnostics,
        IEngineeringWorkflowTracePreviewService tracePreviewService)
    {
        _inputSnapshotBuilder = inputSnapshotBuilder;
        _workflowDiagnostics = workflowDiagnostics;
        _tracePreviewService = tracePreviewService;
    }

    public async Task<EngineeringWorkflowStateDto> BuildWorkflowStateAsync(
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

        var snapshot = await _inputSnapshotBuilder.BuildAsync(
            projectId,
            buildingId,
            EngineeringWorkflowCatalog.DefaultWeatherYear,
            cancellationToken);

        var projectResult = snapshot.ProjectResult;
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

        var buildingsResult = snapshot.BuildingsResult;
        if (buildingsResult.IsFailure)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_BUILDINGS_LIST_UNAVAILABLE",
                Message: buildingsResult.Error,
                SourceStep: "Building",
                SuggestedCorrection: "Ensure project/building persistence is available."));
        }

        var selectedBuildingId = snapshot.SelectedBuildingId;

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
                Steps: _workflowDiagnostics.BuildStepStatusesForMissingBuilding(diagnostics),
                AvailableModules: EngineeringWorkflowCatalog.AvailableModules,
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
                Diagnostics: _workflowDiagnostics.SortAndDistinctDiagnostics(diagnostics).ToArray(),
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
                    ["buildingSelection"] = "missing",
                    ["inputSnapshot"] = "resolved"
                });
        }

        var buildingResult = snapshot.BuildingResult ?? Result<BuildingResponse>.Failure("Building snapshot is unavailable.");
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

        var roomsResult = snapshot.RoomsResult ?? Result<List<RoomResponse>>.Failure("Rooms snapshot is unavailable.");
        var zonesResult = snapshot.ZonesResult ?? Result<List<ThermalZoneResponse>>.Failure("Thermal zones snapshot is unavailable.");
        var readinessResult = snapshot.ReadinessResult ?? Result<BuildingCalculationReadinessReport>.Failure("Building readiness snapshot is unavailable.");
        var validationResult = snapshot.ValidationResult ?? Result<BuildingValidationReport>.Failure("Building validation snapshot is unavailable.");
        var coreStatusResult = snapshot.CoreStatusResult;

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
        var walls = snapshot.Walls;
        var windows = snapshot.Windows;
        var ventilationConfiguredRoomCount = snapshot.VentilationConfiguredRoomCount;
        var groundConfiguredRoomCount = snapshot.GroundConfiguredRoomCount;

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
                Indicator: _workflowDiagnostics.MapBoundaryIndicator(wall.BoundaryType),
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
                    Severity: _workflowDiagnostics.NormalizeSeverity(issue.Severity),
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
                    Severity: _workflowDiagnostics.NormalizeSeverity(issue.Severity),
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

        var diagnosticsSorted = _workflowDiagnostics.SortAndDistinctDiagnostics(diagnostics).ToArray();
        var buildingArea = rooms.Sum(room => room.AreaM2);
        var buildingVolume = rooms.Sum(room => room.VolumeM3);

        var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["mode"] = "api",
            ["stage"] = "foundation",
            ["weatherYear"] = EngineeringWorkflowCatalog.DefaultWeatherYear.ToString(),
            ["buildingSelection"] = "resolved",
            ["inputSnapshot"] = "resolved"
        };

        var provisionalState = new EngineeringWorkflowStateDto(
            ProjectId: projectId,
            ProjectName: projectName,
            BuildingId: building.Id,
            CurrentStep: "Project",
            Steps: [],
            AvailableModules: EngineeringWorkflowCatalog.AvailableModules,
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

        var traceSummary = _tracePreviewService.BuildTraceSummary(
            _tracePreviewService.BuildTraceDocument(provisionalState, CalculationTraceDetailLevel.Summary, diagnosticsSorted),
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
            CurrentStep = _workflowDiagnostics.SelectCurrentStep(zoneDtos, boundaryDtos, diagnosticsSorted),
            Steps = _workflowDiagnostics.BuildStepStatuses(provisionalState, diagnosticsSorted),
            WeatherSolarSettings = new EngineeringWorkflowWeatherSolarSettingsDto(
                WeatherSourceStatus: readinessResult.IsSuccess && readinessResult.Value.IsReady ? "Ready" : "Requires fixes",
                LocationTimezoneSummary: $"Weather year {EngineeringWorkflowCatalog.DefaultWeatherYear}, climate: {building.ClimateZoneName ?? "n/a"}",
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

    public EngineeringWorkflowStateDto BuildInfrastructureFallbackState(
        int projectId,
        int? buildingId,
        string errorMessage)
    {
        var diagnostics = _workflowDiagnostics.SortAndDistinctDiagnostics(
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
            AvailableModules: EngineeringWorkflowCatalog.AvailableModules,
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
                ["persistenceProvider"] = "Unavailable",
                ["durablePersistenceEnabled"] = "false",
                ["fallback"] = "deterministic"
            });

        var steps = _workflowDiagnostics.BuildStepStatuses(state, diagnostics);
        return state with { Steps = steps };
    }
}
