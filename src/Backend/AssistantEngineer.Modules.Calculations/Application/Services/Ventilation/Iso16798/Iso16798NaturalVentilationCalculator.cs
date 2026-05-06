using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationCalculator
{
    private const double GravityMPerS2 = 9.81;
    private const double CelsiusToKelvin = 273.15;
    private const double MinimumKelvin = 1.0;

    public Iso16798NaturalVentilationResult Calculate(Iso16798NaturalVentilationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<Iso16798NaturalVentilationDiagnostics>();
        ValidateInput(input, diagnostics);

        var effectiveOpeningArea = ResolveEffectiveOpeningArea(input, diagnostics);
        var temperatureDeltaC = Math.Abs(input.IndoorTemperatureC - input.OutdoorTemperatureC);
        var averageTemperatureK = Math.Max(
            ((input.IndoorTemperatureC + CelsiusToKelvin) + (input.OutdoorTemperatureC + CelsiusToKelvin)) / 2.0,
            MinimumKelvin);

        var usefulHeight = Math.Max(input.UsefulHeightDifferenceM, input.OpeningHeightM);
        usefulHeight = Math.Max(usefulHeight, 0.0);

        var dischargeCoefficient = Math.Clamp(input.DischargeCoefficient, 0.0, 1.0);
        if (!NearlyEqual(dischargeCoefficient, input.DischargeCoefficient))
        {
            diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                "Iso16798Ventilation.DischargeCoefficientClamped",
                $"Discharge coefficient was clamped to {dischargeCoefficient:F6}."));
        }

        var stackCoefficient = Math.Max(input.StackCoefficient, 0.0);
        var windCoefficient = Math.Max(input.WindCoefficient, 0.0);
        var windSpeed = Math.Max(input.WindSpeedMPerS, 0.0);
        var windExposure = Math.Max(input.WindExposureFactor, 0.0);
        var windPressureCoefficient = Math.Abs(input.WindPressureCoefficient);

        var stackAirflow = 0.0;
        if (effectiveOpeningArea > 0.0 && temperatureDeltaC > 0.0 && usefulHeight > 0.0 && stackCoefficient > 0.0)
        {
            var stackVelocity =
                Math.Sqrt(2.0 * GravityMPerS2 * usefulHeight * (temperatureDeltaC / averageTemperatureK));
            stackAirflow = effectiveOpeningArea * dischargeCoefficient * stackCoefficient * stackVelocity;
        }

        var windAirflow = 0.0;
        if (effectiveOpeningArea > 0.0 && windSpeed > 0.0 && windCoefficient > 0.0 && windExposure > 0.0 && windPressureCoefficient > 0.0)
        {
            var windVelocityFactor = Math.Sqrt(windPressureCoefficient * windExposure);
            windAirflow = effectiveOpeningArea * dischargeCoefficient * windCoefficient * windSpeed * windVelocityFactor;
        }

        var totalAirflowM3PerS = Math.Max(stackAirflow + windAirflow, 0.0);
        var totalAirflowM3PerH = totalAirflowM3PerS * 3600.0;
        var ach = input.RoomVolumeM3 > 0.0
            ? totalAirflowM3PerH / input.RoomVolumeM3
            : 0.0;

        var maxAch = Math.Max(input.MaximumAirChangesPerHour, 0.0);
        var clampedAch = Math.Clamp(ach, 0.0, maxAch);

        if (clampedAch < ach)
        {
            diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                "Iso16798Ventilation.AchClamped",
                $"Air changes per hour was clamped from {ach:F6} to {clampedAch:F6}."));
        }

        var clampedFlowM3PerS = clampedAch * input.RoomVolumeM3 / 3600.0;
        var heatTransferCoefficient =
            input.AirDensityKgPerM3 *
            input.AirSpecificHeatJPerKgK *
            clampedFlowM3PerS;

        var mode = ResolveMode(effectiveOpeningArea, stackAirflow, windAirflow);
        diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
            "Iso16798Ventilation.CalculationMode",
            $"Calculation mode: {mode}."));

        diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
            "Iso16798Ventilation.Summary",
            $"Effective area {effectiveOpeningArea:F6} m2, stack {stackAirflow:F6} m3/s, wind {windAirflow:F6} m3/s, total {totalAirflowM3PerS:F6} m3/s."));

        return new Iso16798NaturalVentilationResult(
            CalculationMode: mode,
            EffectiveOpeningAreaM2: Round6(effectiveOpeningArea),
            StackAirflowM3PerS: Round6(stackAirflow),
            WindAirflowM3PerS: Round6(windAirflow),
            TotalAirflowM3PerS: Round6(totalAirflowM3PerS),
            TotalAirflowM3PerH: Round6(totalAirflowM3PerH),
            AirChangesPerHour: Round6(ach),
            ClampedAirChangesPerHour: Round6(clampedAch),
            HeatTransferCoefficientWPerK: Round6(heatTransferCoefficient),
            Diagnostics: diagnostics);
    }

    private static void ValidateInput(
        Iso16798NaturalVentilationInput input,
        List<Iso16798NaturalVentilationDiagnostics> diagnostics)
    {
        if (input.Openings is null || input.Openings.Count == 0)
        {
            throw new InvalidOperationException("Iso16798 natural ventilation input requires at least one opening.");
        }

        if (input.RoomVolumeM3 <= 0.0)
            throw new InvalidOperationException("Iso16798 natural ventilation input requires RoomVolumeM3 > 0.");

        if (input.AirDensityKgPerM3 <= 0.0)
            throw new InvalidOperationException("Iso16798 natural ventilation input requires AirDensityKgPerM3 > 0.");

        if (input.AirSpecificHeatJPerKgK <= 0.0)
            throw new InvalidOperationException("Iso16798 natural ventilation input requires AirSpecificHeatJPerKgK > 0.");

        if (input.MaximumAirChangesPerHour < 0.0)
            throw new InvalidOperationException("Iso16798 natural ventilation input requires MaximumAirChangesPerHour >= 0.");

        if (input.UsefulHeightDifferenceM < 0.0)
        {
            diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                "Iso16798Ventilation.UsefulHeightNegative",
                "UsefulHeightDifferenceM was negative and treated as zero for stack airflow."));
        }

        if (input.OpeningHeightM < 0.0)
        {
            diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                "Iso16798Ventilation.OpeningHeightNegative",
                "OpeningHeightM was negative and treated as zero for stack airflow."));
        }
    }

    private static double ResolveEffectiveOpeningArea(
        Iso16798NaturalVentilationInput input,
        List<Iso16798NaturalVentilationDiagnostics> diagnostics)
    {
        var effectiveArea = 0.0;

        foreach (var opening in input.Openings)
        {
            if (!opening.IsOpen)
                continue;

            var area = opening.OpeningAreaM2;
            if (area < 0.0)
            {
                diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                    "Iso16798Ventilation.OpeningAreaClamped",
                    $"Opening '{opening.OpeningId}' had negative area and was clamped to zero."));
                area = 0.0;
            }

            var ratio = opening.OpeningRatio;
            var clampedRatio = Math.Clamp(ratio, 0.0, 1.0);
            if (!NearlyEqual(clampedRatio, ratio))
            {
                diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                    "Iso16798Ventilation.OpeningRatioClamped",
                    $"Opening '{opening.OpeningId}' ratio was clamped from {ratio:F6} to {clampedRatio:F6}."));
            }

            effectiveArea += area * clampedRatio;
        }

        return effectiveArea;
    }

    private static Iso16798NaturalVentilationCalculationMode ResolveMode(
        double effectiveOpeningArea,
        double stackAirflow,
        double windAirflow)
    {
        if (effectiveOpeningArea <= 0.0 || (stackAirflow <= 0.0 && windAirflow <= 0.0))
            return Iso16798NaturalVentilationCalculationMode.ClosedOpenings;

        if (stackAirflow > 0.0 && windAirflow > 0.0)
            return Iso16798NaturalVentilationCalculationMode.StackAndWind;

        if (stackAirflow > 0.0)
            return Iso16798NaturalVentilationCalculationMode.StackOnly;

        return Iso16798NaturalVentilationCalculationMode.WindOnly;
    }

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static bool NearlyEqual(double left, double right) =>
        Math.Abs(left - right) < 1e-12;
}
