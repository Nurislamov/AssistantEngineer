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
// DHW/system-energy/readiness validation extracted without changing diagnostic semantics.
public sealed partial class BuildingInputValidationService
{


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
}