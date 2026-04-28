using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomEnvelopeInputCalculator : IIso52016RoomEnvelopeInputCalculator
{
    public Result<Iso52016RoomEnvelopeInput> Calculate(
        Room room,
        Iso52016RoomSimulationDefaults defaults)
    {
        if (room is null)
            return Result<Iso52016RoomEnvelopeInput>.Validation("Room is required.");

        if (defaults is null)
            return Result<Iso52016RoomEnvelopeInput>.Validation("Room simulation defaults are required.");

        var transmission = CalculateTransmissionHeatTransferCoefficient(room);
        var ventilation = CalculateVentilationHeatTransferCoefficient(room, defaults);
        var capacity = CalculateThermalCapacity(room, defaults);

        if (transmission <= 0 && ventilation <= 0)
        {
            return Result<Iso52016RoomEnvelopeInput>.Validation(
                "Room must have at least one heat transfer path.");
        }

        return Result<Iso52016RoomEnvelopeInput>.Success(
            new Iso52016RoomEnvelopeInput(
                TransmissionHeatTransferCoefficientWPerK: transmission,
                VentilationHeatTransferCoefficientWPerK: ventilation,
                ThermalCapacityJPerK: capacity));
    }

    private static double CalculateTransmissionHeatTransferCoefficient(
        Room room)
    {
        var wallTransmission = room.Walls
            .Where(wall => wall.BoundaryType is WallBoundaryType.External or WallBoundaryType.Ground or WallBoundaryType.AdjacentUnconditioned)
            .Sum(wall =>
                wall.Area.SquareMeters *
                wall.UValue.Value);

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