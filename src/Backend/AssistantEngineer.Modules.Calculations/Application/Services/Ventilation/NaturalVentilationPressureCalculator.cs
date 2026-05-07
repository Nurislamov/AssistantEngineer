using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationPressureCalculator : INaturalVentilationPressureCalculator
{
    private const double DefaultAirDensityKgPerCubicMeter = 1.204;
    private const double GravityMetersPerSecondSquared = 9.80665;
    private const double CelsiusToKelvin = 273.15;

    public NaturalVentilationPressureResult CalculateWindPressure(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(opening);
        ArgumentNullException.ThrowIfNull(environment);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var density = ResolveDensity(environment.OutdoorAirDensityKgPerCubicMeter, diagnostics, "wind");

        if (!double.IsFinite(environment.WindSpeedMetersPerSecond) || environment.WindSpeedMetersPerSecond < 0.0)
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-PRESSURE-NOT-CALCULABLE",
                $"Opening '{opening.OpeningId}' wind pressure is not calculable due to invalid wind speed."));
            return new NaturalVentilationPressureResult(
                PressureDifferencePa: null,
                AirDensityKgPerCubicMeter: density,
                Diagnostics: diagnostics);
        }

        if (!opening.WindPressureCoefficient.HasValue && !opening.OppositeWindPressureCoefficient.HasValue)
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-WIND-PRESSURE-DATA-MISSING",
                $"Opening '{opening.OpeningId}' wind pressure coefficients are missing."));
            diagnostics.Add(CreateWarning(
                "AE-VENT-PRESSURE-NOT-CALCULABLE",
                $"Opening '{opening.OpeningId}' wind pressure is not calculable due to missing Cp metadata."));
            return new NaturalVentilationPressureResult(
                PressureDifferencePa: null,
                AirDensityKgPerCubicMeter: density,
                Diagnostics: diagnostics);
        }

        var cp = opening.WindPressureCoefficient ?? 0.0;
        var oppositeCp = opening.OppositeWindPressureCoefficient ?? 0.0;
        var deltaCp = opening.OppositeWindPressureCoefficient.HasValue ? cp - oppositeCp : cp;
        var windPressure = 0.5 * density * Math.Pow(environment.WindSpeedMetersPerSecond, 2.0) * deltaCp;

        diagnostics.Add(CreateInfo(
            "AE-VENT-WIND-PRESSURE-CALCULATED",
            $"Opening '{opening.OpeningId}' wind pressure was calculated using deterministic pressure coefficient inputs."));

        return new NaturalVentilationPressureResult(
            PressureDifferencePa: windPressure,
            AirDensityKgPerCubicMeter: density,
            Diagnostics: diagnostics);
    }

    public NaturalVentilationPressureResult CalculateStackPressure(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(opening);
        ArgumentNullException.ThrowIfNull(environment);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var outdoorDensity = ResolveDensity(environment.OutdoorAirDensityKgPerCubicMeter, diagnostics, "stack-outdoor");
        var indoorDensity = ResolveDensity(
            environment.IndoorAirDensityKgPerCubicMeter ?? environment.OutdoorAirDensityKgPerCubicMeter,
            diagnostics,
            "stack-indoor");
        var density = 0.5 * (outdoorDensity + indoorDensity);

        var stackHeight = ResolveStackHeight(opening, environment);
        if (!(stackHeight > 0.0))
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-STACK-HEIGHT-MISSING",
                $"Opening '{opening.OpeningId}' stack pressure requires a resolvable height difference."));
            diagnostics.Add(CreateWarning(
                "AE-VENT-PRESSURE-NOT-CALCULABLE",
                $"Opening '{opening.OpeningId}' stack pressure is not calculable due to missing height metadata."));
            return new NaturalVentilationPressureResult(
                PressureDifferencePa: null,
                AirDensityKgPerCubicMeter: density,
                Diagnostics: diagnostics);
        }

        if (!double.IsFinite(environment.IndoorTemperatureCelsius) ||
            !double.IsFinite(environment.OutdoorTemperatureCelsius))
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-PRESSURE-NOT-CALCULABLE",
                $"Opening '{opening.OpeningId}' stack pressure is not calculable due to invalid indoor/outdoor temperatures."));
            return new NaturalVentilationPressureResult(
                PressureDifferencePa: null,
                AirDensityKgPerCubicMeter: density,
                Diagnostics: diagnostics);
        }

        var deltaTemperature = environment.IndoorTemperatureCelsius - environment.OutdoorTemperatureCelsius;
        if (Math.Abs(deltaTemperature) <= 0.0)
        {
            diagnostics.Add(CreateInfo(
                "AE-VENT-STACK-PRESSURE-CALCULATED",
                $"Opening '{opening.OpeningId}' stack pressure is zero because indoor and outdoor temperatures are equal."));
            return new NaturalVentilationPressureResult(
                PressureDifferencePa: 0.0,
                AirDensityKgPerCubicMeter: density,
                Diagnostics: diagnostics);
        }

        var indoorK = environment.IndoorTemperatureCelsius + CelsiusToKelvin;
        var outdoorK = environment.OutdoorTemperatureCelsius + CelsiusToKelvin;
        var referenceTemperatureK = Math.Max(1.0, 0.5 * (indoorK + outdoorK));
        var stackPressure = density * GravityMetersPerSecondSquared * stackHeight *
                            Math.Abs(deltaTemperature / referenceTemperatureK);

        diagnostics.Add(CreateInfo(
            "AE-VENT-STACK-PRESSURE-CALCULATED",
            $"Opening '{opening.OpeningId}' stack pressure was calculated using deterministic buoyancy assumptions."));

        return new NaturalVentilationPressureResult(
            PressureDifferencePa: stackPressure,
            AirDensityKgPerCubicMeter: density,
            Diagnostics: diagnostics);
    }

    public NaturalVentilationPressureResult CalculateCombinedPressure(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(opening);
        ArgumentNullException.ThrowIfNull(environment);

        var wind = CalculateWindPressure(opening, environment);
        var stack = CalculateStackPressure(opening, environment);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(wind.Diagnostics);
        diagnostics.AddRange(stack.Diagnostics);

        var windPressure = wind.PressureDifferencePa.GetValueOrDefault();
        var stackPressure = stack.PressureDifferencePa.GetValueOrDefault();

        if (!wind.PressureDifferencePa.HasValue && !stack.PressureDifferencePa.HasValue)
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-PRESSURE-NOT-CALCULABLE",
                $"Opening '{opening.OpeningId}' combined pressure is not calculable because both wind and stack pressures are unavailable."));
            return new NaturalVentilationPressureResult(
                PressureDifferencePa: null,
                AirDensityKgPerCubicMeter: wind.AirDensityKgPerCubicMeter,
                Diagnostics: diagnostics);
        }

        var combined = Math.Sqrt(windPressure * windPressure + stackPressure * stackPressure);
        diagnostics.Add(CreateInfo(
            "AE-VENT-COMBINED-PRESSURE-RSS-USED",
            $"Opening '{opening.OpeningId}' combined pressure uses root-sum-square of wind and stack components."));

        return new NaturalVentilationPressureResult(
            PressureDifferencePa: combined,
            AirDensityKgPerCubicMeter: wind.AirDensityKgPerCubicMeter,
            Diagnostics: diagnostics);
    }

    private static double ResolveDensity(
        double? density,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        string context)
    {
        if (density.HasValue && double.IsFinite(density.Value) && density.Value > 0.0)
        {
            return density.Value;
        }

        diagnostics.Add(CreateInfo(
            "AE-VENT-AIR-DENSITY-DEFAULTED",
            $"Air density was defaulted to {DefaultAirDensityKgPerCubicMeter:F3} kg/m3 ({context})."));
        return DefaultAirDensityKgPerCubicMeter;
    }

    private static double ResolveStackHeight(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment)
    {
        if (opening.TopHeightMeters.HasValue &&
            opening.BottomHeightMeters.HasValue &&
            double.IsFinite(opening.TopHeightMeters.Value) &&
            double.IsFinite(opening.BottomHeightMeters.Value))
        {
            var topBottomHeight = opening.TopHeightMeters.Value - opening.BottomHeightMeters.Value;
            if (topBottomHeight > 0.0)
                return topBottomHeight;
        }

        if (opening.OpeningHeightMeters is > 0.0)
            return opening.OpeningHeightMeters.Value;

        if (opening.OpeningCenterHeightMeters.HasValue &&
            environment.OpeningReferenceHeightMeters.HasValue &&
            double.IsFinite(opening.OpeningCenterHeightMeters.Value) &&
            double.IsFinite(environment.OpeningReferenceHeightMeters.Value))
        {
            var centerReferenceHeight =
                Math.Abs(opening.OpeningCenterHeightMeters.Value - environment.OpeningReferenceHeightMeters.Value);
            if (centerReferenceHeight > 0.0)
                return centerReferenceHeight;
        }

        return 0.0;
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.BoundaryCondition,
            "NaturalVentilationPressureCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.BoundaryCondition,
            "NaturalVentilationPressureCalculator");
}
