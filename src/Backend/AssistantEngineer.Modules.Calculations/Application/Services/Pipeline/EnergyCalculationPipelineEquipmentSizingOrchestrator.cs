using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal sealed class EnergyCalculationPipelineEquipmentSizingOrchestrator
{
    private readonly EquipmentSizingEngine _equipmentSizingEngine;

    public EnergyCalculationPipelineEquipmentSizingOrchestrator(
        EquipmentSizingEngine equipmentSizingEngine)
    {
        _equipmentSizingEngine = equipmentSizingEngine;
    }

    public async Task<Result<EquipmentSizingResult>> CalculateForRoomAsync(
        Room room,
        RoomLoadCalculationResult load,
        CalculationPreferences preferences,
        string systemType,
        string unitType,
        ICoolingEquipmentCatalogSizingProvider equipmentCatalogSizingProvider,
        CancellationToken cancellationToken)
    {
        var catalog = await equipmentCatalogSizingProvider.ListActiveCoolingCandidatesAsync(
            systemType,
            unitType,
            cancellationToken);

        var candidates = catalog
            .Select(candidate => new EquipmentSizingCandidateInput(
                candidate.CatalogItemId,
                candidate.Manufacturer,
                candidate.ModelName,
                $"{candidate.SystemType}/{candidate.UnitType}",
                HeatingCapacityW: candidate.NominalHeatingCapacityKw * 1000,
                CoolingCapacityW: candidate.NominalCoolingCapacityKw * 1000,
                IsActive: true))
            .ToArray();

        var canEvaluateHeating = load.HeatingLoadW > 0 &&
            catalog.Any(candidate => candidate.NominalHeatingCapacityKw.HasValue);
        var evaluatedHeatingLoadW = canEvaluateHeating ? load.HeatingLoadW : 0;

        var sizing = _equipmentSizingEngine.Calculate(new EquipmentSizingInput(
            TargetId: room.Id,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: evaluatedHeatingLoadW,
            RequiredCoolingLoadW: load.CoolingLoadW,
            SafetyFactor: preferences.CoolingSafetyFactor,
            Candidates: candidates,
            DiagnosticsContext: $"Room {room.Id} equipment selection",
            HeatingSafetyFactor: preferences.HeatingSafetyFactor,
            CoolingSafetyFactor: preferences.CoolingSafetyFactor));

        if (sizing.IsFailure)
            return sizing;

        var diagnostics = sizing.Value.Diagnostics.ToList();
        if (load.HeatingLoadW > 0 && !canEvaluateHeating)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "EquipmentSizing.HeatingCapacityUnavailable",
                "Heating sizing is skipped because catalog items do not expose heating capacity.",
                $"Room {room.Id} equipment selection"));
        }

        return Result<EquipmentSizingResult>.Success(sizing.Value with
        {
            RequiredHeatingCapacityW = Round(load.HeatingLoadW),
            RequiredHeatingCapacityWithReserveW = Round(load.HeatingLoadW * preferences.HeatingSafetyFactor),
            Diagnostics = diagnostics
        });
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
