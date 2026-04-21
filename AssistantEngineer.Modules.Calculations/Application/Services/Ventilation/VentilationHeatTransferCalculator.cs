using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public interface IVentilationHeatTransferCalculator
{
    double Calculate(Room room, VentilationCalculationContext context);
    double CalculateMechanical(Room room, VentilationCalculationContext context);
    double CalculateInfiltration(Room room, VentilationCalculationContext context);
}

public sealed class VentilationHeatTransferCalculator : IVentilationHeatTransferCalculator
{
    private const double AirHeatCapacityWhPerM3K = 0.34;
    private readonly IIso16798ReferenceData _referenceData;

    public VentilationHeatTransferCalculator(IIso16798ReferenceData referenceData)
    {
        _referenceData = referenceData;
    }

    public double Calculate(Room room, VentilationCalculationContext context)
    {
        return CalculateMechanical(room, context) + CalculateInfiltration(room, context);
    }

    public double CalculateMechanical(Room room, VentilationCalculationContext context)
    {
        var heatRecoveryFactor = 1 - (room.VentilationParameters?.HeatRecoveryEfficiency ?? 0);
        var volume = room.CalculateVolume();
        var airChangesPerHour = context.Method switch
        {
            VentilationCalculationMethod.Occupancy => CalculateOccupancyAirChanges(room),
            VentilationCalculationMethod.Custom => context.CustomHeatTransferCoefficientWPerK.HasValue
                ? context.CustomHeatTransferCoefficientWPerK.Value / Math.Max(AirHeatCapacityWhPerM3K * volume, 0.001)
                : room.VentilationParameters?.AirChangesPerHour ?? 0.5,
            _ => room.VentilationParameters?.AirChangesPerHour ?? 0.5
        };

        return Math.Max(0, AirHeatCapacityWhPerM3K * airChangesPerHour * volume * heatRecoveryFactor);
    }

    public double CalculateInfiltration(Room room, VentilationCalculationContext context)
    {
        var parameters = room.VentilationParameters;
        if (parameters is null)
            return 0;

        var volume = room.CalculateVolume();
        var stackEffect = parameters.StackCoefficient *
            Math.Sqrt(Math.Abs(context.IndoorTemperatureC - context.OutdoorTemperatureC));
        var windEffect = parameters.WindCoefficient *
            Math.Max(context.WindSpeedMPerS, 0) *
            parameters.WindExposureFactor;
        var airChangesPerHour = parameters.InfiltrationAirChangesPerHour +
            stackEffect +
            windEffect;

        return Math.Max(0, AirHeatCapacityWhPerM3K * airChangesPerHour * volume);
    }

    private double CalculateOccupancyAirChanges(Room room)
    {
        var defaults = _referenceData.GetRoomDefaults(room.Type);
        var flowM3PerHour = defaults.MinimumVentilationLitersPerSecondM2 *
            room.Area.SquareMeters *
            3.6;
        return flowM3PerHour / Math.Max(room.CalculateVolume(), 0.001);
    }

}

public enum VentilationCalculationMethod
{
    FixedAirChanges,
    Occupancy,
    TemperatureWind,
    Custom
}

public sealed record VentilationCalculationContext(
    VentilationCalculationMethod Method,
    double IndoorTemperatureC,
    double OutdoorTemperatureC,
    double WindSpeedMPerS = 0,
    double? CustomHeatTransferCoefficientWPerK = null);
