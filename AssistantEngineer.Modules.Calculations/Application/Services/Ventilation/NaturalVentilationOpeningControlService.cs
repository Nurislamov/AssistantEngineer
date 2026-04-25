using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationOpeningControlService : INaturalVentilationOpeningControlService
{
    private readonly NaturalVentilationOptions _options;

    public NaturalVentilationOpeningControlService(
        IOptions<NaturalVentilationOptions> options)
    {
        _options = options.Value;
    }

    public NaturalVentilationOpeningState Resolve(
        Room room,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeedMPerS,
        double demandFactor,
        int hourOfDay)
    {
        if (!_options.Enabled)
            return Closed("Natural ventilation disabled.");

        if (room.Windows.Count == 0)
            return Closed("Room has no windows.");

        if (demandFactor < _options.MinimumDemandFactor)
            return Closed("Ventilation demand factor is too low.");

        if (outdoorTemperatureC < _options.MinimumOutdoorTemperatureC ||
            outdoorTemperatureC > _options.MaximumOutdoorTemperatureC)
            return Closed("Outdoor temperature is outside opening range.");

        if (windSpeedMPerS > _options.MaximumWindSpeedForOpeningMPerS)
            return Closed("Wind speed is above opening limit.");

        var totalWindowArea = room.Windows.Sum(window => window.Area.SquareMeters);
        var operableArea = totalWindowArea * Math.Clamp(_options.OperableWindowAreaFraction, 0.0, 1.0);

        if (operableArea <= 0.0)
            return Closed("Operable opening area is zero.");

        var indoorOutdoorDelta = indoorTemperatureC - outdoorTemperatureC;

        var daytimeCoolingAllowed =
            indoorTemperatureC >= _options.IndoorTemperatureThresholdC &&
            indoorOutdoorDelta >= _options.MinimumIndoorOutdoorDeltaC;

        var nightCoolingAllowed =
            _options.EnableNightCooling &&
            IsNightCoolingHour(hourOfDay) &&
            indoorTemperatureC >= _options.NightCoolingIndoorTemperatureThresholdC &&
            outdoorTemperatureC < indoorTemperatureC;

        if (!daytimeCoolingAllowed && !nightCoolingAllowed)
            return Closed("Opening criteria are not satisfied.");

        var openingFactor = Math.Clamp(demandFactor, 0.0, 1.0);

        if (nightCoolingAllowed && !daytimeCoolingAllowed)
            openingFactor = Math.Max(openingFactor, _options.MinimumNightOpeningFactor);

        var effectiveArea = operableArea * openingFactor;

        return new NaturalVentilationOpeningState(
            IsOpen: effectiveArea > 0.0,
            OpeningFactor: openingFactor,
            EffectiveOpeningAreaM2: effectiveArea,
            Reason: nightCoolingAllowed
                ? "Night cooling opening."
                : "Daytime free cooling opening.");
    }

    private bool IsNightCoolingHour(int hourOfDay)
    {
        var start = _options.NightCoolingStartHour;
        var end = _options.NightCoolingEndHour;

        if (start == end)
            return true;

        return start > end
            ? hourOfDay >= start || hourOfDay < end
            : hourOfDay >= start && hourOfDay < end;
    }

    private static NaturalVentilationOpeningState Closed(string reason) =>
        new(
            IsOpen: false,
            OpeningFactor: 0.0,
            EffectiveOpeningAreaM2: 0.0,
            Reason: reason);
}