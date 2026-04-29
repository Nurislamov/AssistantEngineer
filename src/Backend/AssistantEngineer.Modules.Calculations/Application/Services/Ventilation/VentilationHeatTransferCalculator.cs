using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class VentilationHeatTransferCalculator : IVentilationHeatTransferCalculator
{
    private readonly IIso16798ReferenceData _referenceData;
    private readonly VentilationAndInfiltrationLoadEngine _engine;

    public VentilationHeatTransferCalculator(
        IIso16798ReferenceData referenceData,
        VentilationAndInfiltrationLoadEngine? engine = null)
    {
        _referenceData = referenceData;
        _engine = engine ?? new VentilationAndInfiltrationLoadEngine();
    }

    public double Calculate(
        Room room,
        VentilationCalculationContext context) =>
        CalculateMechanical(room, context) + CalculateInfiltration(room, context);

    public double CalculateMechanical(
        Room room,
        VentilationCalculationContext context)
    {
        if (context.CustomHeatTransferCoefficientWPerK.HasValue &&
            context.Method == VentilationCalculationMethod.Custom)
        {
            return Math.Max(0, context.CustomHeatTransferCoefficientWPerK.Value);
        }

        var heatRecoveryEfficiency = room.VentilationParameters?.HeatRecoveryEfficiency ?? 0;
        var input = context.Method == VentilationCalculationMethod.Occupancy
            ? CreateBaseInput(room) with
            {
                AirflowPerAreaLpsM2 = _referenceData.GetRoomDefaults(room.Type).MinimumVentilationLitersPerSecondM2,
                HeatRecoveryEfficiency = heatRecoveryEfficiency
            }
            : CreateBaseInput(room) with
            {
                AirChangesPerHour = room.VentilationParameters?.AirChangesPerHour ?? 0.5,
                HeatRecoveryEfficiency = heatRecoveryEfficiency
            };

        var result = _engine.Calculate(input);
        return result.Value.MechanicalVentilation.EffectiveHeatingLoadW;
    }

    public double CalculateInfiltration(
        Room room,
        VentilationCalculationContext context)
    {
        var parameters = room.VentilationParameters;
        if (parameters is null)
            return 0;

        var stackEffect = parameters.StackCoefficient *
            Math.Sqrt(Math.Abs(context.IndoorTemperatureC - context.OutdoorTemperatureC));
        var windEffect = parameters.WindCoefficient *
            Math.Max(context.WindSpeedMPerS, 0) *
            parameters.WindExposureFactor;
        var airChangesPerHour = parameters.InfiltrationAirChangesPerHour +
            stackEffect +
            windEffect;

        var result = _engine.Calculate(
            CreateBaseInput(room) with
            {
                InfiltrationAirChangesPerHour = airChangesPerHour
            });

        return result.Value.Infiltration.HeatingLoadW;
    }

    private static VentilationAndInfiltrationLoadInput CreateBaseInput(
        Room room) =>
        new(
            RoomId: room.Id,
            AreaM2: room.Area.SquareMeters,
            VolumeM3: room.CalculateVolume(),
            OccupancyPeople: room.PeopleCount,
            IndoorTemperatureC: 1,
            OutdoorTemperatureC: 0,
            DiagnosticsContext: $"Room {room.Id} ventilation heat transfer");
}
