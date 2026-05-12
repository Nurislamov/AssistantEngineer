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
// Shared diagnostic/result factory and deterministic helper methods.
public sealed partial class BuildingInputValidationService
{


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