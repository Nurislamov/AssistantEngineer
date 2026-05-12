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
// Room/floor geometry validation extracted without changing diagnostic semantics.
public sealed partial class BuildingInputValidationService
{


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
}