using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationAirflowService : INaturalVentilationAirflowService
{
    private readonly NaturalVentilationOptions _naturalOptions;
    private readonly INaturalVentilationOpeningControlService _openingControl;

    public NaturalVentilationAirflowService(
        IOptions<NaturalVentilationOptions> naturalOptions,
        INaturalVentilationOpeningControlService openingControl)
    {
        _naturalOptions = naturalOptions.Value;
        _openingControl = openingControl;
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

        var dischargeCoefficient = Math.Clamp(_naturalOptions.OpeningDischargeCoefficient, 0.01, 1.0);
        var stackCoefficient = Math.Max(0.0, ventilation.StackCoefficient);
        var windCoefficient = Math.Max(0.0, ventilation.WindCoefficient);
        var windExposureFactor = Math.Max(1.0, ventilation.WindExposureFactor);

        var deltaT = Math.Abs(indoorTemperatureC - outdoorTemperatureC);

        var stackFlowM3PerS =
            opening.EffectiveOpeningAreaM2 *
            dischargeCoefficient *
            stackCoefficient *
            Math.Sqrt(Math.Max(deltaT, 0.0));

        var windFlowM3PerS =
            opening.EffectiveOpeningAreaM2 *
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
}
