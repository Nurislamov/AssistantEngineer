using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.InputQuality;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InputQuality;
using AssistantEngineer.SharedKernel.Diagnostics;
using AssistantEngineer.SharedKernel.Primitives;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.InputQuality;

public sealed class EngineeringInputQualityService : IEngineeringInputQualityService
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ICalculationPreferencesRepository _calculationPreferencesRepository;
    private readonly ILogger<EngineeringInputQualityService> _logger;

    public EngineeringInputQualityService(
        IBuildingRepository buildingRepository,
        IRoomRepository roomRepository,
        ICalculationPreferencesRepository calculationPreferencesRepository,
        ILogger<EngineeringInputQualityService>? logger = null)
    {
        _buildingRepository = buildingRepository;
        _roomRepository = roomRepository;
        _calculationPreferencesRepository = calculationPreferencesRepository;
        _logger = logger ?? NullLogger<EngineeringInputQualityService>.Instance;
    }

    public async Task<Result<EngineeringInputQualityReport>> CheckBuildingInputQualityAsync(
        int buildingId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "{EventCode} Input quality check started. SubjectType={SubjectType} SubjectId={SubjectId} Scope={Scope} CorrelationId={CorrelationId}",
            ObservabilityEventCodes.InputQualityCheckStarted,
            "Building",
            buildingId,
            "BuildingInputQuality",
            "n/a");

        var building = await _buildingRepository.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
        {
            _logger.LogWarning(
                "{EventCode} Input quality check failed because subject was not found. SubjectType={SubjectType} SubjectId={SubjectId} Scope={Scope} ErrorCode={ErrorCode} DurationMs={DurationMs} CorrelationId={CorrelationId}",
                ObservabilityEventCodes.InputQualityBlockingIssueDetected,
                "Building",
                buildingId,
                "BuildingInputQuality",
                "IQ-BLD-001",
                stopwatch.ElapsedMilliseconds,
                "n/a");
            return Result<EngineeringInputQualityReport>.NotFound($"Building with id {buildingId} not found.");
        }

        var diagnostics = new List<EngineeringInputQualityDiagnostic>();
        EvaluateBuildingLevel(building, diagnostics);

        var rooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .OrderBy(room => room.Id)
            .ToArray();

        foreach (var room in rooms)
            EvaluateRoomLevel(room, diagnostics);

        await AddDefaultsUsageDiagnosticIfNeededAsync(
            projectId: building.ProjectId,
            subjectCodePrefix: "IQ-BLD",
            diagnostics,
            cancellationToken);

        var report = BuildReport(
            scope: "BuildingInputQuality",
            subjectType: "Building",
            subjectId: building.Id,
            diagnostics);

        LogInputQualityCompletion(report, stopwatch.ElapsedMilliseconds);
        return Result<EngineeringInputQualityReport>.Success(report);
    }

    public async Task<Result<EngineeringInputQualityReport>> CheckRoomInputQualityAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "{EventCode} Input quality check started. SubjectType={SubjectType} SubjectId={SubjectId} Scope={Scope} CorrelationId={CorrelationId}",
            ObservabilityEventCodes.InputQualityCheckStarted,
            "Room",
            roomId,
            "RoomInputQuality",
            "n/a");

        var room = await _roomRepository.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning(
                "{EventCode} Input quality check failed because subject was not found. SubjectType={SubjectType} SubjectId={SubjectId} Scope={Scope} ErrorCode={ErrorCode} DurationMs={DurationMs} CorrelationId={CorrelationId}",
                ObservabilityEventCodes.InputQualityBlockingIssueDetected,
                "Room",
                roomId,
                "RoomInputQuality",
                "IQ-ROOM-001",
                stopwatch.ElapsedMilliseconds,
                "n/a");
            return Result<EngineeringInputQualityReport>.NotFound($"Room with id {roomId} not found.");
        }

        var diagnostics = new List<EngineeringInputQualityDiagnostic>();
        EvaluateRoomLevel(room, diagnostics);

        var projectId = room.Floor?.Building?.ProjectId ?? 0;
        await AddDefaultsUsageDiagnosticIfNeededAsync(
            projectId: projectId,
            subjectCodePrefix: "IQ-ROOM",
            diagnostics,
            cancellationToken);

        var report = BuildReport(
            scope: "RoomInputQuality",
            subjectType: "Room",
            subjectId: room.Id,
            diagnostics);

        LogInputQualityCompletion(report, stopwatch.ElapsedMilliseconds);
        return Result<EngineeringInputQualityReport>.Success(report);
    }

    private void LogInputQualityCompletion(EngineeringInputQualityReport report, long durationMs)
    {
        _logger.LogInformation(
            "{EventCode} Input quality check completed. SubjectType={SubjectType} SubjectId={SubjectId} Scope={Scope} DiagnosticCount={DiagnosticCount} HighestSeverity={HighestSeverity} HasBlockingIssues={HasBlockingIssues} IsCalculationReady={IsCalculationReady} ResultStatus={ResultStatus} DurationMs={DurationMs} CorrelationId={CorrelationId}",
            ObservabilityEventCodes.InputQualityCheckCompleted,
            report.SubjectType,
            report.SubjectId,
            report.Scope,
            report.Diagnostics.Count,
            report.HighestSeverity.ToString(),
            report.HasBlockingIssues,
            report.IsCalculationReady,
            report.IsCalculationReady ? "Ready" : "NotReady",
            durationMs,
            "n/a");

        if (report.HasBlockingIssues)
        {
            _logger.LogWarning(
                "{EventCode} Input quality blocking issue detected. SubjectType={SubjectType} SubjectId={SubjectId} Scope={Scope} DiagnosticCount={DiagnosticCount} HighestSeverity={HighestSeverity} DurationMs={DurationMs} CorrelationId={CorrelationId}",
                ObservabilityEventCodes.InputQualityBlockingIssueDetected,
                report.SubjectType,
                report.SubjectId,
                report.Scope,
                report.Diagnostics.Count,
                report.HighestSeverity.ToString(),
                durationMs,
                "n/a");
        }
    }

    private static void EvaluateBuildingLevel(
        Building building,
        ICollection<EngineeringInputQualityDiagnostic> diagnostics)
    {
        if (building.Floors.Count == 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-BLD-010",
                severity: EngineeringInputQualitySeverity.Blocking,
                category: EngineeringInputQualityCategory.CalculationReadiness,
                message: "Building has no floors.",
                field: "building.floors",
                recommendation: "Add at least one floor before running engineering calculations."));
        }

        var rooms = building.Floors.SelectMany(floor => floor.Rooms).ToArray();
        if (rooms.Length == 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-BLD-011",
                severity: EngineeringInputQualitySeverity.Blocking,
                category: EngineeringInputQualityCategory.CalculationReadiness,
                message: "Building has no rooms.",
                field: "building.floors[].rooms",
                recommendation: "Add at least one room before running engineering calculations."));
        }

        if (building.ClimateZone is null)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-BLD-020",
                severity: EngineeringInputQualitySeverity.Warning,
                category: EngineeringInputQualityCategory.Weather,
                message: "Building climate zone/context is missing.",
                field: "building.climateZone",
                recommendation: "Assign a climate zone or equivalent climate context for weather-dependent calculations."));
        }

        var roomsWithGroundBoundaryButNoMetadata = rooms
            .Where(room =>
                room.GroundContactMetadata is null &&
                room.Walls.Any(wall => wall.BoundaryType == WallBoundaryType.Ground))
            .Select(room => room.Id)
            .ToArray();

        if (roomsWithGroundBoundaryButNoMetadata.Length > 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-BLD-030",
                severity: EngineeringInputQualitySeverity.Warning,
                category: EngineeringInputQualityCategory.Ground,
                message: "Ground-contact boundaries were found without explicit ground metadata.",
                field: "building.floors[].rooms[].groundContactMetadata",
                recommendation: "Provide ground-contact metadata for rooms with ground boundaries.",
                metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["roomIds"] = string.Join(",", roomsWithGroundBoundaryButNoMetadata.OrderBy(id => id))
                }));
        }
    }

    private static void EvaluateRoomLevel(
        Room room,
        ICollection<EngineeringInputQualityDiagnostic> diagnostics)
    {
        if (room.Area.SquareMeters <= 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ROOM-010",
                severity: EngineeringInputQualitySeverity.Blocking,
                category: EngineeringInputQualityCategory.Geometry,
                message: "Room floor area must be positive.",
                field: "room.areaM2",
                unit: "m²",
                recommendation: "Set room area to a value greater than 0."));
        }

        if (room.HeightM <= 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ROOM-011",
                severity: EngineeringInputQualitySeverity.Blocking,
                category: EngineeringInputQualityCategory.Geometry,
                message: "Room height must be positive.",
                field: "room.heightM",
                unit: "m",
                recommendation: "Set room height to a value greater than 0."));
        }

        var volumeM3 = room.CalculateVolume();
        if (volumeM3 <= 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ROOM-012",
                severity: EngineeringInputQualitySeverity.Blocking,
                category: EngineeringInputQualityCategory.Geometry,
                message: "Room volume must be positive or derivable from area and height.",
                field: "room.volumeM3",
                unit: "m³",
                recommendation: "Provide valid room area and height so room volume is positive."));
        }

        if (room.Walls.Count == 0 && room.Windows.Count == 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ROOM-020",
                severity: EngineeringInputQualitySeverity.Warning,
                category: EngineeringInputQualityCategory.Envelope,
                message: "Room envelope input is missing: no walls or windows found.",
                field: "room.envelope",
                recommendation: "Add envelope elements or document why the room is intentionally excluded."));
        }

        var totalWindowAreaM2 = room.Windows.Sum(window => window.Area.SquareMeters);
        if (room.Area.SquareMeters > 0 && totalWindowAreaM2 > room.Area.SquareMeters * 0.8)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ROOM-030",
                severity: EngineeringInputQualitySeverity.Warning,
                category: EngineeringInputQualityCategory.Envelope,
                message: "Window-to-floor area ratio is suspiciously high (> 0.8).",
                field: "room.windows[].areaM2",
                unit: "m²",
                recommendation: "Verify glazing areas and floor area input values.",
                metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["totalWindowAreaM2"] = totalWindowAreaM2.ToString("G17", CultureInfo.InvariantCulture),
                    ["floorAreaM2"] = room.Area.SquareMeters.ToString("G17", CultureInfo.InvariantCulture)
                }));
        }

        if (room.VentilationParameters is null)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ROOM-040",
                severity: EngineeringInputQualitySeverity.Warning,
                category: EngineeringInputQualityCategory.Ventilation,
                message: "Room ventilation configuration is missing and defaults may be used.",
                field: "room.ventilation",
                recommendation: "Provide room ventilation parameters or confirm default assumptions."));
        }
        else
        {
            var ventilation = room.VentilationParameters;
            if (ventilation.AirChangesPerHour < 0 || ventilation.InfiltrationAirChangesPerHour < 0)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "IQ-ROOM-041",
                    severity: EngineeringInputQualitySeverity.Error,
                    category: EngineeringInputQualityCategory.Ventilation,
                    message: "Ventilation airflow inputs must not be negative.",
                    field: "room.ventilation.airChangesPerHour",
                    unit: "ACH",
                    recommendation: "Set non-negative ACH and infiltration ACH values."));
            }
        }

        foreach (var wall in room.Walls)
        {
            if (wall.UValue.Value <= 0)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "IQ-ROOM-050",
                    severity: EngineeringInputQualitySeverity.Error,
                    category: EngineeringInputQualityCategory.Envelope,
                    message: "Envelope U-value must be positive.",
                    field: "room.walls[].uValueWPerM2K",
                    unit: "W/(m²·K)",
                    recommendation: "Set a positive U-value for all envelope elements.",
                    metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["wallId"] = wall.Id.ToString(CultureInfo.InvariantCulture)
                    }));
            }
            else if (wall.UValue.Value > 10)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "IQ-ROOM-051",
                    severity: EngineeringInputQualitySeverity.Warning,
                    category: EngineeringInputQualityCategory.Envelope,
                    message: "Envelope U-value is suspiciously high (> 10 W/(m²·K)).",
                    field: "room.walls[].uValueWPerM2K",
                    unit: "W/(m²·K)",
                    recommendation: "Review wall U-value data source and units.",
                    metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["wallId"] = wall.Id.ToString(CultureInfo.InvariantCulture),
                        ["uValueWPerM2K"] = wall.UValue.Value.ToString("G17", CultureInfo.InvariantCulture)
                    }));
            }
        }

        foreach (var window in room.Windows)
        {
            if (window.UValue.Value <= 0)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "IQ-ROOM-050",
                    severity: EngineeringInputQualitySeverity.Error,
                    category: EngineeringInputQualityCategory.Envelope,
                    message: "Envelope U-value must be positive.",
                    field: "room.windows[].uValueWPerM2K",
                    unit: "W/(m²·K)",
                    recommendation: "Set a positive U-value for all window elements.",
                    metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["windowId"] = window.Id.ToString(CultureInfo.InvariantCulture)
                    }));
            }
            else if (window.UValue.Value > 10)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "IQ-ROOM-051",
                    severity: EngineeringInputQualitySeverity.Warning,
                    category: EngineeringInputQualityCategory.Envelope,
                    message: "Envelope U-value is suspiciously high (> 10 W/(m²·K)).",
                    field: "room.windows[].uValueWPerM2K",
                    unit: "W/(m²·K)",
                    recommendation: "Review window U-value data source and units.",
                    metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["windowId"] = window.Id.ToString(CultureInfo.InvariantCulture),
                        ["uValueWPerM2K"] = window.UValue.Value.ToString("G17", CultureInfo.InvariantCulture)
                    }));
            }

            if (window.Shgc.Value is < 0 or > 1)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "IQ-ROOM-060",
                    severity: EngineeringInputQualitySeverity.Error,
                    category: EngineeringInputQualityCategory.Solar,
                    message: "Window SHGC must be within the 0..1 range.",
                    field: "room.windows[].shgc",
                    unit: "dimensionless",
                    recommendation: "Set SHGC between 0 and 1.",
                    metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["windowId"] = window.Id.ToString(CultureInfo.InvariantCulture),
                        ["shgc"] = window.Shgc.Value.ToString("G17", CultureInfo.InvariantCulture)
                    }));
            }
        }

        if (room.PeopleCount < 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ROOM-080",
                severity: EngineeringInputQualitySeverity.Error,
                category: EngineeringInputQualityCategory.InternalGains,
                message: "People count cannot be negative.",
                field: "room.peopleCount",
                recommendation: "Set people count to zero or a positive value."));
        }

        if (room.IndoorTemperature is null)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ROOM-070",
                severity: EngineeringInputQualitySeverity.Warning,
                category: EngineeringInputQualityCategory.Assumptions,
                message: "Room indoor setpoint is missing and defaults may be used.",
                field: "room.indoorTemperatureC",
                unit: "°C",
                recommendation: "Provide an explicit indoor setpoint or document the fallback assumption."));
        }
    }

    private async Task AddDefaultsUsageDiagnosticIfNeededAsync(
        int projectId,
        string subjectCodePrefix,
        ICollection<EngineeringInputQualityDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        if (projectId <= 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ASSUMP-001",
                severity: EngineeringInputQualitySeverity.Warning,
                category: EngineeringInputQualityCategory.Assumptions,
                message: "Calculation preference defaults may be used because project context is missing.",
                field: "project.id",
                source: subjectCodePrefix,
                recommendation: "Persist and link project context to resolve explicit calculation preferences."));
            return;
        }

        var preferences = await _calculationPreferencesRepository.GetByProjectIdAsync(projectId, cancellationToken);
        if (preferences is null)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "IQ-ASSUMP-001",
                severity: EngineeringInputQualitySeverity.Warning,
                category: EngineeringInputQualityCategory.Assumptions,
                message: "Calculation preference defaults are in use because explicit preferences were not found.",
                field: "calculationPreferences",
                source: subjectCodePrefix,
                recommendation: "Create project calculation preferences to make assumptions explicit.",
                metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["projectId"] = projectId.ToString(CultureInfo.InvariantCulture)
                }));
        }
    }

    private static EngineeringInputQualityReport BuildReport(
        string scope,
        string subjectType,
        int? subjectId,
        IReadOnlyList<EngineeringInputQualityDiagnostic> diagnostics)
    {
        var orderedDiagnostics = diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ToArray();

        var hasBlockingIssues = orderedDiagnostics.Any(diagnostic =>
            diagnostic.Severity == EngineeringInputQualitySeverity.Blocking);
        var hasErrors = orderedDiagnostics.Any(diagnostic =>
            diagnostic.Severity == EngineeringInputQualitySeverity.Error);
        var hasWarnings = orderedDiagnostics.Any(diagnostic =>
            diagnostic.Severity == EngineeringInputQualitySeverity.Warning);

        var highestSeverity = orderedDiagnostics.Length == 0
            ? EngineeringInputQualitySeverity.Info
            : orderedDiagnostics.MaxBy(diagnostic => diagnostic.Severity)!.Severity;

        return new EngineeringInputQualityReport(
            Scope: scope,
            SubjectType: subjectType,
            SubjectId: subjectId,
            Diagnostics: orderedDiagnostics,
            HighestSeverity: highestSeverity,
            HasBlockingIssues: hasBlockingIssues,
            HasWarnings: hasWarnings,
            IsCalculationReady: !hasBlockingIssues && !hasErrors);
    }

    private static EngineeringInputQualityDiagnostic CreateDiagnostic(
        string code,
        EngineeringInputQualitySeverity severity,
        string category,
        string message,
        string? field = null,
        string? unit = null,
        string? recommendation = null,
        string? source = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new EngineeringInputQualityDiagnostic(
            Code: code,
            Severity: severity,
            Message: message,
            Category: category,
            Field: field,
            Unit: unit,
            Recommendation: recommendation,
            Source: source,
            Metadata: metadata);
    }
}
