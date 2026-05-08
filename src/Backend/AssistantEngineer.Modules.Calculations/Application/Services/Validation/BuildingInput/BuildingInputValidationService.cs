using AssistantEngineer.Modules.Buildings.Domain.Construction;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;

public sealed class BuildingInputValidationService
{
    private static readonly IReadOnlyList<string> ClaimBoundary =
    [
        "Building input validation and correction framework.",
        "Internal deterministic engineering governance only.",
        "No automatic production data mutation.",
        "No full ISO/EN compliance claim.",
        "No StandardReference equivalence claim.",
        "No EnergyPlus comparison workflow claim.",
        "No ASHRAE 140 / BESTEST-style validation anchor claim.",
        "No external certification claim."
    ];

    private readonly Iso52016ConstructionOptions _constructionOptions;

    public BuildingInputValidationService(
        IOptions<Iso52016ConstructionOptions>? constructionOptions = null)
    {
        _constructionOptions = constructionOptions?.Value ?? new Iso52016ConstructionOptions();
    }

    public BuildingInputValidationResult Validate(Building building) =>
        Validate(new BuildingInputValidationRequest(building));

    public BuildingInputValidationResult Validate(BuildingInputValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<BuildingInputValidationDiagnostic>();
        if (request.Building is null)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.BuildingRequired",
                severity: BuildingInputValidationSeverity.Critical,
                category: BuildingInputValidationCategory.DataCompleteness,
                scope: BuildingInputValidationScope.Building,
                targetPath: "$.building",
                message: "Building input is required."));
            return BuildResult(diagnostics, request.EvaluateIso52016Readiness);
        }

        ValidateGeometry(request.Building, diagnostics);
        ValidateEnvelope(request.Building, diagnostics);
        ValidateOpenings(request.Building, diagnostics);
        ValidateVentilation(request.Building, diagnostics);
        ValidateGround(request.Building, diagnostics);
        ValidateConstruction(request.Building, request, diagnostics);
        ValidateDomesticHotWater(request, diagnostics);
        ValidateSystemEnergy(request, diagnostics);

        if (request.EvaluateIso52016Readiness)
            ValidateIso52016Readiness(request.Building, diagnostics);

        return BuildResult(diagnostics, request.EvaluateIso52016Readiness);
    }

    private static void ValidateGeometry(
        Building building,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        if (building.Floors.Count == 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Geometry.BuildingHasNoFloors",
                severity: BuildingInputValidationSeverity.Critical,
                category: BuildingInputValidationCategory.Geometry,
                scope: BuildingInputValidationScope.Building,
                targetPath: "$.building.floors",
                message: "Building must contain at least one floor."));
            return;
        }

        foreach (var (floor, floorIndex) in building.Floors.Select((item, index) => (item, index)))
        {
            var floorPath = $"$.building.floors[{floorIndex}]";
            if (floor.Rooms.Count == 0)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Geometry.FloorHasNoRooms",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Geometry,
                    scope: BuildingInputValidationScope.Floor,
                    targetPath: $"{floorPath}.rooms",
                    message: $"Floor '{floor.Name}' must contain at least one room."));
                continue;
            }

            foreach (var (room, roomIndex) in floor.Rooms.Select((item, index) => (item, index)))
            {
                var roomPath = $"{floorPath}.rooms[{roomIndex}]";
                if (!(room.Area.SquareMeters > 0.0))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Geometry.RoomAreaNonPositive",
                        severity: BuildingInputValidationSeverity.Error,
                        category: BuildingInputValidationCategory.Geometry,
                        scope: BuildingInputValidationScope.Room,
                        targetPath: $"{roomPath}.area",
                        message: $"Room '{room.Name}' area must be greater than zero."));
                }

                if (!(room.HeightM > 0.0))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Geometry.RoomHeightNonPositive",
                        severity: BuildingInputValidationSeverity.Error,
                        category: BuildingInputValidationCategory.Geometry,
                        scope: BuildingInputValidationScope.Room,
                        targetPath: $"{roomPath}.heightM",
                        message: $"Room '{room.Name}' height must be greater than zero.",
                        suggestedCorrection: new BuildingInputSuggestedCorrection(
                            CorrectionId: "BIV-CORR-ROOM-HEIGHT-DEFAULT-3M",
                            TargetPath: $"{roomPath}.heightM",
                            Description: "Set room height to a deterministic default of 3.0 m.",
                            ProposedValue: "3.0",
                            IsAutomaticSafe: false,
                            RequiresUserReview: true)));
                }

                if (!(room.CalculateVolume() > 0.0))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Geometry.RoomVolumeNonPositive",
                        severity: BuildingInputValidationSeverity.Error,
                        category: BuildingInputValidationCategory.Geometry,
                        scope: BuildingInputValidationScope.Room,
                        targetPath: $"{roomPath}.volume",
                        message: $"Room '{room.Name}' volume must be greater than zero."));
                }
            }
        }
    }

    private static void ValidateEnvelope(
        Building building,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        foreach (var (room, roomPath) in EnumerateRooms(building))
        {
            var hasHeatTransferWall = room.Walls.Any(IsHeatTransferWall);
            var hasWindow = room.Windows.Any();
            var hasVentilationPath = HasVentilationPath(room.VentilationParameters);

            if (!hasHeatTransferWall && !hasWindow && !hasVentilationPath)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Envelope.NoHeatTransferPath",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Envelope,
                    scope: BuildingInputValidationScope.Room,
                    targetPath: roomPath,
                    message: $"Room '{room.Name}' must have at least one heat transfer path (walls, windows, or ventilation)."));
            }

            foreach (var (wall, wallIndex) in room.Walls.Select((item, index) => (item, index)))
            {
                var wallPath = $"{roomPath}.walls[{wallIndex}]";
                if (!(wall.Area.SquareMeters > 0.0))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Envelope.WallAreaNonPositive",
                        severity: BuildingInputValidationSeverity.Error,
                        category: BuildingInputValidationCategory.Envelope,
                        scope: BuildingInputValidationScope.Wall,
                        targetPath: $"{wallPath}.area",
                        message: "Wall area must be greater than zero."));
                }

                if (!(wall.UValue.Value > 0.0))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Envelope.WallUValueNonPositive",
                        severity: BuildingInputValidationSeverity.Error,
                        category: BuildingInputValidationCategory.Envelope,
                        scope: BuildingInputValidationScope.Wall,
                        targetPath: $"{wallPath}.uValue",
                        message: "Wall U-value must be greater than zero."));
                }

                if (!Enum.IsDefined(wall.BoundaryType))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Envelope.WallBoundaryTypeInvalid",
                        severity: BuildingInputValidationSeverity.Error,
                        category: BuildingInputValidationCategory.BoundaryConditions,
                        scope: BuildingInputValidationScope.Wall,
                        targetPath: $"{wallPath}.boundaryType",
                        message: "Wall boundary type must be a defined value."));
                }

                if (wall.BoundaryType == WallBoundaryType.External &&
                    !Enum.IsDefined(wall.Orientation))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Envelope.ExternalWallOrientationMissing",
                        severity: BuildingInputValidationSeverity.Warning,
                        category: BuildingInputValidationCategory.BoundaryConditions,
                        scope: BuildingInputValidationScope.Wall,
                        targetPath: $"{wallPath}.orientation",
                        message: "External wall orientation is missing or invalid.",
                        suggestedCorrection: new BuildingInputSuggestedCorrection(
                            CorrectionId: "BIV-CORR-WALL-ORIENTATION-REVIEW",
                            TargetPath: $"{wallPath}.orientation",
                            Description: "Provide a valid external wall orientation.",
                            ProposedValue: null,
                            IsAutomaticSafe: false,
                            RequiresUserReview: true)));
                }
            }
        }
    }

    private static void ValidateOpenings(
        Building building,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        foreach (var (room, roomPath) in EnumerateRooms(building))
        {
            foreach (var (window, windowIndex) in room.Windows.Select((item, index) => (item, index)))
            {
                var windowPath = $"{roomPath}.windows[{windowIndex}]";
                if (!(window.Area.SquareMeters > 0.0))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Openings.WindowAreaNonPositive",
                        severity: BuildingInputValidationSeverity.Error,
                        category: BuildingInputValidationCategory.Openings,
                        scope: BuildingInputValidationScope.Window,
                        targetPath: $"{windowPath}.area",
                        message: "Window area must be greater than zero."));
                }

                if (!(window.UValue.Value > 0.0))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Openings.WindowUValueNonPositive",
                        severity: BuildingInputValidationSeverity.Error,
                        category: BuildingInputValidationCategory.Openings,
                        scope: BuildingInputValidationScope.Window,
                        targetPath: $"{windowPath}.uValue",
                        message: "Window U-value must be greater than zero."));
                }

                if (!Enum.IsDefined(window.Orientation))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Openings.WindowOrientationMissing",
                        severity: BuildingInputValidationSeverity.Warning,
                        category: BuildingInputValidationCategory.Openings,
                        scope: BuildingInputValidationScope.Window,
                        targetPath: $"{windowPath}.orientation",
                        message: "Window orientation is missing or invalid.",
                        suggestedCorrection: new BuildingInputSuggestedCorrection(
                            CorrectionId: "BIV-CORR-WINDOW-ORIENTATION-REVIEW",
                            TargetPath: $"{windowPath}.orientation",
                            Description: "Provide a valid window orientation.",
                            ProposedValue: null,
                            IsAutomaticSafe: false,
                            RequiresUserReview: true)));
                }

                if (window.Shgc.Value is < 0.0 or > 1.0 || double.IsNaN(window.Shgc.Value))
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Openings.WindowShgcOutOfRange",
                        severity: BuildingInputValidationSeverity.Error,
                        category: BuildingInputValidationCategory.Openings,
                        scope: BuildingInputValidationScope.Window,
                        targetPath: $"{windowPath}.shgc",
                        message: "Window SHGC must be between 0 and 1.",
                        suggestedCorrection: new BuildingInputSuggestedCorrection(
                            CorrectionId: "BIV-CORR-WINDOW-SHGC-CLAMP",
                            TargetPath: $"{windowPath}.shgc",
                            Description: "Clamp SHGC value into [0, 1].",
                            ProposedValue: Clamp01(window.Shgc.Value).ToString("0.###"),
                            IsAutomaticSafe: true,
                            RequiresUserReview: true)));
                }
            }

            var externalWallAreaByFacade = room.Walls
                .Where(wall => wall.BoundaryType == WallBoundaryType.External)
                .GroupBy(wall => NormalizeFacade(wall.Orientation))
                .ToDictionary(group => group.Key, group => group.Sum(wall => wall.Area.SquareMeters));
            var windowAreaByFacade = room.Windows
                .GroupBy(window => NormalizeFacade(window.Orientation))
                .ToDictionary(group => group.Key, group => group.Sum(window => window.Area.SquareMeters));

            foreach (var (facade, windowArea) in windowAreaByFacade)
            {
                if (!externalWallAreaByFacade.TryGetValue(facade, out var wallArea) || wallArea <= 0.0)
                    continue;

                if (windowArea > wallArea)
                {
                    diagnostics.Add(CreateDiagnostic(
                        code: "BuildingInput.Openings.WindowAreaExceedsRelatedExternalWallArea",
                        severity: BuildingInputValidationSeverity.Warning,
                        category: BuildingInputValidationCategory.Openings,
                        scope: BuildingInputValidationScope.Room,
                        targetPath: $"{roomPath}.windows",
                        message: $"Window area on facade '{facade}' exceeds related external wall area."));
                }
            }
        }
    }

    private static void ValidateVentilation(
        Building building,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        foreach (var (room, roomPath) in EnumerateRooms(building))
        {
            var ventilation = room.VentilationParameters;
            var ventilationPath = $"{roomPath}.ventilationParameters";
            if (ventilation is null)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Ventilation.MechanicalAndNaturalVentilationMissing",
                    severity: BuildingInputValidationSeverity.Warning,
                    category: BuildingInputValidationCategory.Ventilation,
                    scope: BuildingInputValidationScope.Ventilation,
                    targetPath: ventilationPath,
                    message: "Mechanical and natural ventilation inputs are both missing."));
                continue;
            }

            ValidateVentilationParameters(ventilation, ventilationPath, diagnostics);

            var hasMechanical = ventilation.AirChangesPerHour > 0.0;
            var inferredNaturalOpeningAreaM2 = Math.Max(0.0, room.Windows.Sum(window => window.Area.SquareMeters)) * 0.25;
            if (inferredNaturalOpeningAreaM2 < 0.0)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Ventilation.NaturalOpeningAreaNegative",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Ventilation,
                    scope: BuildingInputValidationScope.Room,
                    targetPath: $"{roomPath}.windows",
                    message: "Inferred natural ventilation effective opening area cannot be negative."));
            }

            if (!hasMechanical && inferredNaturalOpeningAreaM2 <= 0.0)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Ventilation.MechanicalAndNaturalVentilationMissing",
                    severity: BuildingInputValidationSeverity.Warning,
                    category: BuildingInputValidationCategory.Ventilation,
                    scope: BuildingInputValidationScope.Room,
                    targetPath: roomPath,
                    message: "Mechanical and natural ventilation inputs are both missing."));
            }
        }
    }

    private static void ValidateGround(
        Building building,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        foreach (var (room, roomPath) in EnumerateRooms(building))
        {
            var hasGroundBoundary = room.Walls.Any(wall => wall.BoundaryType == WallBoundaryType.Ground);
            if (!hasGroundBoundary)
                continue;

            if (room.GroundContactMetadata is null)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Ground.MetadataMissingForGroundBoundary",
                    severity: BuildingInputValidationSeverity.Warning,
                    category: BuildingInputValidationCategory.Ground,
                    scope: BuildingInputValidationScope.Ground,
                    targetPath: $"{roomPath}.groundContactMetadata",
                    message: "Ground-contact metadata is missing for room with ground boundary; fallback assumptions may be used.",
                    suggestedCorrection: new BuildingInputSuggestedCorrection(
                        CorrectionId: "BIV-CORR-GROUND-METADATA-ADD",
                        TargetPath: $"{roomPath}.groundContactMetadata",
                        Description: "Provide deterministic ground-contact metadata (contact type, exposed perimeter, burial depth).",
                        ProposedValue: null,
                        IsAutomaticSafe: false,
                        RequiresUserReview: true)));
                continue;
            }

            ValidateGroundMetadata(room, room.GroundContactMetadata, roomPath, diagnostics);
        }
    }

    private void ValidateConstruction(
        Building building,
        BuildingInputValidationRequest request,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        var constructionOptInIntended =
            request.IsConstructionLayerMassOptInIntended ||
            _constructionOptions.UseConstructionLayerMassInput;

        foreach (var (room, roomPath) in EnumerateRooms(building))
        {
            var heatTransferWalls = room.Walls.Where(IsHeatTransferWall).ToArray();
            foreach (var (wall, wallIndex) in heatTransferWalls.Select((item, index) => (item, index)))
            {
                var wallPath = $"{roomPath}.walls[{wallIndex}]";
                ValidateConstructionAssembly(wall.ConstructionAssembly, wallPath, diagnostics);
            }

            if (!constructionOptInIntended)
                continue;

            if (heatTransferWalls.Length == 0)
                continue;

            var wallsWithoutLayers = heatTransferWalls
                .Where(wall => wall.ConstructionAssembly is null || wall.ConstructionAssembly.Layers.Count == 0)
                .ToArray();
            if (wallsWithoutLayers.Length == 0)
                continue;

            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Construction.OptInMissingLayersFallbackWillBeUsed",
                severity: BuildingInputValidationSeverity.Warning,
                category: BuildingInputValidationCategory.Construction,
                scope: BuildingInputValidationScope.Construction,
                targetPath: $"{roomPath}.walls",
                message: "Construction layer/mass opt-in is intended, but one or more heat-transfer walls have no construction layers. Equivalent fallback assemblies will be used.",
                suggestedCorrection: new BuildingInputSuggestedCorrection(
                    CorrectionId: "BIV-CORR-CONSTRUCTION-LAYERS-ADD",
                    TargetPath: $"{roomPath}.walls",
                    Description: "Add explicit construction layers or accept equivalent fallback assembly behavior for this scenario.",
                    ProposedValue: "EquivalentFallbackAssembly",
                    IsAutomaticSafe: false,
                    RequiresUserReview: true)));
        }
    }

    private static void ValidateDomesticHotWater(
        BuildingInputValidationRequest request,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        if (!request.DhwExpected)
            return;

        if (request.DhwPeopleCount is null or <= 0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Dhw.PeopleCountMissingOrZero",
                severity: BuildingInputValidationSeverity.Warning,
                category: BuildingInputValidationCategory.Dhw,
                scope: BuildingInputValidationScope.Project,
                targetPath: "$.dhw.peopleCount",
                message: "DHW is expected, but people count is missing or zero."));
        }

        if (request.DhwLitersPerPersonPerDay is null or <= 0.0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Dhw.LitersPerPersonPerDayMissingOrZero",
                severity: BuildingInputValidationSeverity.Warning,
                category: BuildingInputValidationCategory.Dhw,
                scope: BuildingInputValidationScope.Project,
                targetPath: "$.dhw.litersPerPersonPerDay",
                message: "DHW is expected, but liters/person/day is missing or zero."));
        }
    }

    private static void ValidateSystemEnergy(
        BuildingInputValidationRequest request,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        if (!request.SystemEnergyExpected)
            return;

        var usefulEnergyProvided =
            (request.SystemUsefulHeatingEnergyKWh ?? 0.0) > 0.0 ||
            (request.SystemUsefulCoolingEnergyKWh ?? 0.0) > 0.0 ||
            (request.SystemUsefulDhwEnergyKWh ?? 0.0) > 0.0;
        if (!usefulEnergyProvided)
            return;

        var conversionProvided =
            (request.SystemHeatingEfficiency ?? 0.0) > 0.0 ||
            (request.SystemHeatingCop ?? 0.0) > 0.0 ||
            (request.SystemCoolingCop ?? 0.0) > 0.0 ||
            (request.SystemDhwEfficiency ?? 0.0) > 0.0 ||
            (request.SystemDhwCop ?? 0.0) > 0.0 ||
            (request.SystemPrimaryEnergyFactor ?? 0.0) > 0.0;

        if (!conversionProvided)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.SystemEnergy.ConversionFactorsMissing",
                severity: BuildingInputValidationSeverity.Warning,
                category: BuildingInputValidationCategory.SystemEnergy,
                scope: BuildingInputValidationScope.System,
                targetPath: "$.systemEnergy",
                message: "Useful system energy is provided, but no efficiency/COP/primary factor is specified."));
        }
    }

    private static void ValidateIso52016Readiness(
        Building building,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        foreach (var (room, roomPath) in EnumerateRooms(building))
        {
            var hasHeatTransferWalls = room.Walls.Any(IsHeatTransferWall);
            var hasWindows = room.Windows.Any();
            var hasVentilationPath = HasVentilationPath(room.VentilationParameters);

            if (!hasHeatTransferWalls && !hasWindows && !hasVentilationPath)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Iso52016Readiness.RoomInsufficientEnvelopeOrVentilationData",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Iso52016Readiness,
                    scope: BuildingInputValidationScope.Iso52016,
                    targetPath: roomPath,
                    message: "Room lacks sufficient envelope/ventilation data for ISO52016-inspired simulation readiness."));
            }

            var hasConstructionMetadata = room.Walls.Any(wall => IsHeatTransferWall(wall) && wall.ConstructionAssembly is { Layers.Count: > 0 });
            if (!hasConstructionMetadata && room.Walls.Any(IsHeatTransferWall))
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Iso52016Readiness.CompatibilityUValuesOnly",
                    severity: BuildingInputValidationSeverity.Warning,
                    category: BuildingInputValidationCategory.Iso52016Readiness,
                    scope: BuildingInputValidationScope.Iso52016,
                    targetPath: $"{roomPath}.walls",
                    message: "Only compatibility wall U-values are available; no explicit construction metadata is present."));
            }

            var hasWallConstructionCapacity = room.Walls.Any(wall => wall.ConstructionAssembly is not null);
            if (!hasWallConstructionCapacity)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Iso52016Readiness.OnlyFallbackInternalCapacityLikely",
                    severity: BuildingInputValidationSeverity.Warning,
                    category: BuildingInputValidationCategory.Iso52016Readiness,
                    scope: BuildingInputValidationScope.Iso52016,
                    targetPath: roomPath,
                    message: "No wall construction assemblies are present; internal capacity path relies on compatibility defaults/fallback behavior."));
            }
        }
    }

    private static void ValidateVentilationParameters(
        VentilationParameters ventilation,
        string ventilationPath,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        if (ventilation.AirChangesPerHour < 0.0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ventilation.AchNegative",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ventilation,
                scope: BuildingInputValidationScope.Ventilation,
                targetPath: $"{ventilationPath}.airChangesPerHour",
                message: "Ventilation ACH cannot be negative.",
                suggestedCorrection: new BuildingInputSuggestedCorrection(
                    CorrectionId: "BIV-CORR-VENT-ACH-CLAMP-ZERO",
                    TargetPath: $"{ventilationPath}.airChangesPerHour",
                    Description: "Clamp ACH to 0 for deterministic minimum safe bound.",
                    ProposedValue: "0",
                    IsAutomaticSafe: true,
                    RequiresUserReview: true)));
        }

        if (ventilation.HeatRecoveryEfficiency is < 0.0 or > 1.0 || double.IsNaN(ventilation.HeatRecoveryEfficiency))
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ventilation.HeatRecoveryEfficiencyOutOfRange",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ventilation,
                scope: BuildingInputValidationScope.Ventilation,
                targetPath: $"{ventilationPath}.heatRecoveryEfficiency",
                message: "Heat-recovery efficiency must be between 0 and 1."));
        }
    }

    private static void ValidateGroundMetadata(
        Room room,
        GroundContactMetadata metadata,
        string roomPath,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        if (!(room.Area.SquareMeters > 0.0))
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ground.RoomAreaNonPositiveForGroundContact",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ground,
                scope: BuildingInputValidationScope.Ground,
                targetPath: $"{roomPath}.area",
                message: "Ground-contact room area must be greater than zero."));
        }

        if (metadata.ExposedPerimeterM < 0.0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ground.ExposedPerimeterNegative",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ground,
                scope: BuildingInputValidationScope.Ground,
                targetPath: $"{roomPath}.groundContactMetadata.exposedPerimeterM",
                message: "Ground-contact exposed perimeter cannot be negative."));
        }

        if (metadata.BurialDepthM < 0.0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ground.BurialDepthNegative",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ground,
                scope: BuildingInputValidationScope.Ground,
                targetPath: $"{roomPath}.groundContactMetadata.burialDepthM",
                message: "Ground-contact burial depth cannot be negative."));
        }

        if (!Enum.IsDefined(metadata.ContactType))
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ground.ContactTypeInvalid",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ground,
                scope: BuildingInputValidationScope.Ground,
                targetPath: $"{roomPath}.groundContactMetadata.contactType",
                message: "Ground-contact type must be a defined value."));
        }
    }

    private static void ValidateConstructionAssembly(
        ConstructionAssembly? assembly,
        string wallPath,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        if (assembly is null)
            return;

        foreach (var (layer, layerIndex) in assembly.Layers.Select((item, index) => (item, index)))
        {
            var layerPath = $"{wallPath}.constructionAssembly.layers[{layerIndex}]";
            if (!(layer.ThicknessM > 0.0))
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Construction.LayerThicknessNonPositive",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Construction,
                    scope: BuildingInputValidationScope.Construction,
                    targetPath: $"{layerPath}.thicknessM",
                    message: "Construction layer thickness must be greater than zero."));
            }

            if (!(layer.Material.ThermalConductivityWPerMK > 0.0))
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Construction.LayerConductivityNonPositive",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Construction,
                    scope: BuildingInputValidationScope.Construction,
                    targetPath: $"{layerPath}.material.thermalConductivityWPerMK",
                    message: "Construction layer conductivity must be greater than zero."));
            }

            if (!(layer.Material.DensityKgPerM3 > 0.0))
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Construction.LayerDensityNonPositive",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Construction,
                    scope: BuildingInputValidationScope.Construction,
                    targetPath: $"{layerPath}.material.densityKgPerM3",
                    message: "Construction layer density must be greater than zero."));
            }

            if (!(layer.Material.SpecificHeatJPerKgK > 0.0))
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Construction.LayerSpecificHeatNonPositive",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Construction,
                    scope: BuildingInputValidationScope.Construction,
                    targetPath: $"{layerPath}.material.specificHeatJPerKgK",
                    message: "Construction layer specific heat must be greater than zero."));
            }
        }
    }

    private static IEnumerable<(Room room, string path)> EnumerateRooms(Building building)
    {
        foreach (var (floor, floorIndex) in building.Floors.Select((item, index) => (item, index)))
        {
            foreach (var (room, roomIndex) in floor.Rooms.Select((item, index) => (item, index)))
                yield return (room, $"$.building.floors[{floorIndex}].rooms[{roomIndex}]");
        }
    }

    private static bool IsHeatTransferWall(Wall wall) =>
        wall.BoundaryType is WallBoundaryType.External or WallBoundaryType.Ground or WallBoundaryType.AdjacentUnconditioned;

    private static bool HasVentilationPath(VentilationParameters? parameters) =>
        parameters is not null &&
        (parameters.AirChangesPerHour > 0.0 || parameters.InfiltrationAirChangesPerHour > 0.0);

    private static CardinalDirection NormalizeFacade(CardinalDirection orientation) =>
        orientation switch
        {
            CardinalDirection.North or CardinalDirection.NorthEast or CardinalDirection.NorthWest => CardinalDirection.North,
            CardinalDirection.East or CardinalDirection.SouthEast => CardinalDirection.East,
            CardinalDirection.West or CardinalDirection.SouthWest => CardinalDirection.West,
            _ => CardinalDirection.South
        };

    private static double Clamp01(double value) =>
        Math.Clamp(value, 0.0, 1.0);

    private static BuildingInputValidationDiagnostic CreateDiagnostic(
        string code,
        BuildingInputValidationSeverity severity,
        BuildingInputValidationCategory category,
        BuildingInputValidationScope scope,
        string targetPath,
        string message,
        BuildingInputSuggestedCorrection? suggestedCorrection = null) =>
        new(
            Code: code,
            Severity: severity,
            Category: category,
            Scope: scope,
            TargetPath: targetPath,
            Message: message,
            SuggestedCorrection: suggestedCorrection);

    private static BuildingInputValidationResult BuildResult(
        IReadOnlyList<BuildingInputValidationDiagnostic> diagnostics,
        bool readinessEvaluated)
    {
        var grouped = Enum.GetValues<BuildingInputValidationSeverity>()
            .ToDictionary(
                severity => severity,
                severity => (IReadOnlyList<BuildingInputValidationDiagnostic>)diagnostics
                    .Where(item => item.Severity == severity)
                    .ToArray());
        var corrections = diagnostics
            .Where(item => item.SuggestedCorrection is not null)
            .Select(item => item.SuggestedCorrection!)
            .ToArray();

        var hasCritical = grouped[BuildingInputValidationSeverity.Critical].Count > 0;
        var hasErrors = grouped[BuildingInputValidationSeverity.Error].Count > 0;
        var hasWarnings = grouped[BuildingInputValidationSeverity.Warning].Count > 0;

        var readiness = hasCritical
            ? BuildingInputValidationReadinessStatus.BlockedByCriticalErrors
            : hasErrors
                ? BuildingInputValidationReadinessStatus.BlockedByErrors
                : !readinessEvaluated
                    ? BuildingInputValidationReadinessStatus.NotEvaluated
                    : hasWarnings
                        ? BuildingInputValidationReadinessStatus.ReadyWithWarnings
                        : BuildingInputValidationReadinessStatus.Ready;

        return new BuildingInputValidationResult(
            ReadinessStatus: readiness,
            Diagnostics: diagnostics,
            DiagnosticsBySeverity: grouped,
            SuggestedCorrections: corrections,
            ClaimBoundary: ClaimBoundary);
    }
}
