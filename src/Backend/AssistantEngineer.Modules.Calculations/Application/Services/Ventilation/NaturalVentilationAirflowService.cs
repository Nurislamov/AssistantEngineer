using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationAirflowService : INaturalVentilationAirflowService
{
    private readonly NaturalVentilationOptions _naturalOptions;
    private readonly INaturalVentilationOpeningControlService _openingControl;
    private readonly Iso16798NaturalVentilationCalculator _iso16798Calculator;
    private readonly Iso16798NaturalVentilationApplicationAdapter _iso16798Adapter;

    public NaturalVentilationAirflowService(
        IOptions<NaturalVentilationOptions> naturalOptions,
        INaturalVentilationOpeningControlService openingControl,
        Iso16798NaturalVentilationCalculator iso16798Calculator,
        Iso16798NaturalVentilationApplicationAdapter iso16798Adapter)
    {
        _naturalOptions = naturalOptions.Value;
        _openingControl = openingControl;
        _iso16798Calculator = iso16798Calculator;
        _iso16798Adapter = iso16798Adapter;
    }

    public double CalculateHeatTransferCoefficient(
        Room room,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeedMPerS,
        double demandFactor,
        int hourOfDay)
    {
        if (!_naturalOptions.Enabled)
            return 0.0;

        var ventilation = room.VentilationParameters;
        if (ventilation is null)
            return 0.0;

        var opening = _openingControl.Resolve(
            room,
            indoorTemperatureC,
            outdoorTemperatureC,
            windSpeedMPerS,
            demandFactor,
            hourOfDay);

        if (!opening.IsOpen || opening.EffectiveOpeningAreaM2 <= 0.0)
            return 0.0;

        if (!_naturalOptions.UseIso16798InspiredCalculator)
        {
            return CalculateCompatibilityHeatTransferCoefficient(
                room,
                ventilation,
                opening.EffectiveOpeningAreaM2,
                indoorTemperatureC,
                outdoorTemperatureC,
                windSpeedMPerS);
        }

        return CalculateIso16798InspiredHeatTransferCoefficient(
            room,
            opening,
            indoorTemperatureC,
            outdoorTemperatureC,
            windSpeedMPerS);
    }

    private double CalculateCompatibilityHeatTransferCoefficient(
        Room room,
        VentilationParameters ventilation,
        double effectiveOpeningAreaM2,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeedMPerS)
    {
        var dischargeCoefficient = Math.Clamp(_naturalOptions.OpeningDischargeCoefficient, 0.01, 1.0);
        var stackCoefficient = Math.Max(0.0, ventilation.StackCoefficient);
        var windCoefficient = Math.Max(0.0, ventilation.WindCoefficient);
        var windExposureFactor = Math.Max(1.0, ventilation.WindExposureFactor);

        var deltaT = Math.Abs(indoorTemperatureC - outdoorTemperatureC);

        var stackFlowM3PerS =
            effectiveOpeningAreaM2 *
            dischargeCoefficient *
            stackCoefficient *
            Math.Sqrt(Math.Max(deltaT, 0.0));

        var windFlowM3PerS =
            effectiveOpeningAreaM2 *
            dischargeCoefficient *
            windCoefficient *
            Math.Max(windSpeedMPerS, 0.0) *
            windExposureFactor;

        var totalFlowM3PerH = (stackFlowM3PerS + windFlowM3PerS) * 3600.0;
        var roomVolume = Math.Max(room.CalculateVolume(), 0.001);

        var ach = totalFlowM3PerH / roomVolume;
        ach = Math.Clamp(ach, 0.0, _naturalOptions.MaximumAirChangesPerHour);

        return AirPhysicalConstants.AirHeatCapacityWhPerM3K * ach * roomVolume;
    }

    private double CalculateIso16798InspiredHeatTransferCoefficient(
        Room room,
        NaturalVentilationOpeningState opening,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeedMPerS)
    {
        var input = _iso16798Adapter.BuildInput(
            room,
            opening,
            _naturalOptions,
            indoorTemperatureC,
            outdoorTemperatureC,
            windSpeedMPerS);

        var result = _iso16798Calculator.Calculate(input);
        return result.HeatTransferCoefficientWPerK;
    }
}
