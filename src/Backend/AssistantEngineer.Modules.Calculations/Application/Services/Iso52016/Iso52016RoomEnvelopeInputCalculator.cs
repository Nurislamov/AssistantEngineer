using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomEnvelopeInputCalculator : ISo52016RoomEnvelopeInputCalculator
{
    private readonly Iso52016ConstructionOptions _constructionOptions;
    private readonly Iso52016ConstructionAssemblyCalculator? _constructionAssemblyCalculator;
    private readonly Iso52016ConstructionAssemblyApplicationAdapter? _constructionAssemblyAdapter;

    public Iso52016RoomEnvelopeInputCalculator(
        IOptions<Iso52016ConstructionOptions>? constructionOptions = null,
        Iso52016ConstructionAssemblyCalculator? constructionAssemblyCalculator = null,
        Iso52016ConstructionAssemblyApplicationAdapter? constructionAssemblyAdapter = null)
    {
        _constructionOptions = constructionOptions?.Value ?? new Iso52016ConstructionOptions();
        _constructionAssemblyCalculator = constructionAssemblyCalculator;
        _constructionAssemblyAdapter = constructionAssemblyAdapter;
    }

    public Result<Iso52016RoomEnvelopeInput> Calculate(
        Room room,
        Iso52016RoomSimulationDefaults defaults)
    {
        if (room is null)
            return Result<Iso52016RoomEnvelopeInput>.Validation("Room is required.");

        if (defaults is null)
            return Result<Iso52016RoomEnvelopeInput>.Validation("Room simulation defaults are required.");

        var compatibilityTransmission = CalculateTransmissionHeatTransferCoefficient(room);
        var compatibilityCapacity = CalculateThermalCapacity(room, defaults);

        var (transmission, thermalCapacity) = _constructionOptions.UseConstructionLayerMassInput
            ? CalculateConstructionIntegratedEnvelopeInput(
                room,
                defaults,
                compatibilityTransmission,
                compatibilityCapacity)
            : (compatibilityTransmission, compatibilityCapacity);

        var ventilation = CalculateVentilationHeatTransferCoefficient(room, defaults);

        if (transmission <= 0 && ventilation <= 0)
        {
            return Result<Iso52016RoomEnvelopeInput>.Validation(
                "Room must have at least one heat transfer path.");
        }

        return Result<Iso52016RoomEnvelopeInput>.Success(
            new Iso52016RoomEnvelopeInput(
                TransmissionHeatTransferCoefficientWPerK: transmission,
                VentilationHeatTransferCoefficientWPerK: ventilation,
                ThermalCapacityJPerK: thermalCapacity));
    }

    private (double transmissionWPerK, double thermalCapacityJPerK) CalculateConstructionIntegratedEnvelopeInput(
        Room room,
        Iso52016RoomSimulationDefaults defaults,
        double compatibilityTransmissionWPerK,
        double compatibilityCapacityJPerK)
    {
        if (_constructionAssemblyCalculator is null || _constructionAssemblyAdapter is null)
            return (compatibilityTransmissionWPerK, compatibilityCapacityJPerK);

        var heatTransferWalls = room.Walls
            .Where(wall => wall.BoundaryType is WallBoundaryType.External or WallBoundaryType.Ground or WallBoundaryType.AdjacentUnconditioned)
            .ToArray();
        if (heatTransferWalls.Length == 0)
            return (compatibilityTransmissionWPerK, compatibilityCapacityJPerK);

        var assemblies = _constructionAssemblyAdapter.BuildWallAssemblies(room, defaults);
        if (assemblies.Count == 0)
            return (compatibilityTransmissionWPerK, compatibilityCapacityJPerK);

        var compatibilityWindowTransmission = room.Windows
            .Sum(window => window.Area.SquareMeters * window.UValue.Value);

        var mappedAssemblies = heatTransferWalls
            .Zip(assemblies, (wall, assembly) => new { Wall = wall, Assembly = assembly })
            .ToArray();

        double wallTransmission = 0.0;
        double wallEffectiveCapacity = 0.0;
        var hasCalculatedWallCapacity = false;

        foreach (var mapped in mappedAssemblies)
        {
            var compatibilityWallUValue = RoomTransmissionInputFactory.ResolveWallUValue(mapped.Wall);

            try
            {
                var result = _constructionAssemblyCalculator.Calculate(mapped.Assembly);
                var effectiveUValue = _constructionOptions.UseCalculatedAssemblyUValue
                    ? result.UValueWPerM2K
                    : compatibilityWallUValue;

                wallTransmission += mapped.Wall.Area.SquareMeters * effectiveUValue;

                if (_constructionOptions.UseCalculatedAssemblyHeatCapacity && result.EffectiveInternalHeatCapacityJPerM2K > 0.0)
                {
                    wallEffectiveCapacity += mapped.Wall.Area.SquareMeters * result.EffectiveInternalHeatCapacityJPerM2K;
                    hasCalculatedWallCapacity = true;
                }
            }
            catch
            {
                wallTransmission += mapped.Wall.Area.SquareMeters * compatibilityWallUValue;
            }
        }

        foreach (var wall in heatTransferWalls.Skip(mappedAssemblies.Length))
        {
            wallTransmission += wall.Area.SquareMeters * RoomTransmissionInputFactory.ResolveWallUValue(wall);
        }

        var transmission = wallTransmission + compatibilityWindowTransmission;

        if (!_constructionOptions.UseCalculatedAssemblyHeatCapacity || !hasCalculatedWallCapacity)
            return (transmission, compatibilityCapacityJPerK);

        var compatibilityWallCapacityFromDomainAssemblies = heatTransferWalls
            .Where(wall => wall.ConstructionAssembly is not null)
            .Sum(wall => wall.Area.SquareMeters * wall.ConstructionAssembly!.InternalHeatCapacityKjPerM2K * 1000.0);
        var nonWallCompatibilityCapacity = Math.Max(0.0, compatibilityCapacityJPerK - compatibilityWallCapacityFromDomainAssemblies);
        var thermalCapacity = nonWallCompatibilityCapacity + wallEffectiveCapacity;

        return (transmission, thermalCapacity);
    }

    private static double CalculateTransmissionHeatTransferCoefficient(
        Room room)
    {
        var wallTransmission = room.Walls
            .Where(wall => wall.BoundaryType is WallBoundaryType.External or WallBoundaryType.Ground or WallBoundaryType.AdjacentUnconditioned)
            .Sum(wall =>
                wall.Area.SquareMeters *
                RoomTransmissionInputFactory.ResolveWallUValue(wall));

        var windowTransmission = room.Windows
            .Sum(window =>
                window.Area.SquareMeters *
                window.UValue.Value);

        return wallTransmission + windowTransmission;
    }

    private static double CalculateVentilationHeatTransferCoefficient(
        Room room,
        Iso52016RoomSimulationDefaults defaults)
    {
        var ventilation = room.VentilationParameters;

        var airChangesPerHour =
            ventilation?.AirChangesPerHour ??
            defaults.DefaultAirChangesPerHour;

        var infiltrationAirChangesPerHour =
            ventilation?.InfiltrationAirChangesPerHour ??
            0.0;

        var heatRecoveryEfficiency =
            ventilation?.HeatRecoveryEfficiency ??
            defaults.DefaultHeatRecoveryEfficiency;

        var effectiveMechanicalAirChanges =
            airChangesPerHour *
            (1.0 - heatRecoveryEfficiency);

        var totalEffectiveAirChanges =
            effectiveMechanicalAirChanges +
            infiltrationAirChangesPerHour;

        var volumeM3 = room.CalculateVolume();

        return
            defaults.AirHeatCapacityWhPerM3K *
            volumeM3 *
            totalEffectiveAirChanges;
    }

    private static double CalculateThermalCapacity(
        Room room,
        Iso52016RoomSimulationDefaults defaults)
    {
        var calculatedCapacityKjPerK = room.CalculateInternalHeatCapacityKjPerK(
            defaults.FloorHeatCapacityKjPerM2K,
            defaults.CeilingHeatCapacityKjPerM2K);

        if (calculatedCapacityKjPerK <= 0)
        {
            calculatedCapacityKjPerK =
                room.Area.SquareMeters *
                defaults.FallbackInternalHeatCapacityKjPerM2K;
        }

        return calculatedCapacityKjPerK * 1000.0;
    }
}
