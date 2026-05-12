using AssistantEngineer.Modules.Buildings.Domain.Construction;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;

// P3-13: extracted from BuildingInputValidationService to keep the public service as a focused facade.
// Envelope/opening validation extracted without changing diagnostic semantics.
public sealed partial class BuildingInputValidationService
{


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
}