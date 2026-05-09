using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationCalculator
{
    private const double GravityMPerS2 = 9.81;
    private const double CelsiusToKelvin = 273.15;
    private const double MinimumKelvin = 1.0;
    private const double SeaLevelReferenceDensityKgPerM3 = 1.204;
    private const double AltitudeScaleHeightM = 8434.5;

    public Iso16798NaturalVentilationResult Calculate(Iso16798NaturalVentilationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<Iso16798NaturalVentilationDiagnostics>();
        ValidateInput(input, diagnostics);

        var options = input.CalculationOptions ?? new NaturalVentilationCalculationOptions();
        var drivingForces = ResolveDrivingForces(input);
        var resolvedOpenings = ResolveOpenings(input);

        var scheduleFraction = ResolveScheduleFraction(input.OpeningSchedule, diagnostics);
        var occupancyEnabled = ResolveOccupancyEnabled(input.OccupancyControl, diagnostics, out var occupancyControlReason);

        if (scheduleFraction <= 0.0)
        {
            return CreateClosedResult(
                diagnostics,
                controlReason: "Opening schedule is closed for this step.",
                clampReason: null);
        }

        if (!occupancyEnabled)
        {
            return CreateClosedResult(
                diagnostics,
                controlReason: occupancyControlReason ?? "Occupancy control disabled natural ventilation for this step.",
                clampReason: null);
        }

        var effectiveOpeningArea = 0.0;
        var weightedDischargeArea = 0.0;
        var weightedOpeningHeight = 0.0;

        foreach (var opening in resolvedOpenings)
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

            var openingFraction = Math.Clamp(opening.OpeningFraction, 0.0, 1.0);
            if (!NearlyEqual(opening.OpeningFraction, openingFraction))
            {
                diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                    "Iso16798Ventilation.OpeningFractionClamped",
                    $"Opening '{opening.OpeningId}' fraction was clamped from {opening.OpeningFraction:F6} to {openingFraction:F6}."));
            }

            var openingScheduleAdjustedFraction = openingFraction * scheduleFraction;
            var effectiveArea = area * openingScheduleAdjustedFraction;

            var dischargeCoefficient = Math.Clamp(
                opening.DischargeCoefficient ?? input.DischargeCoefficient,
                0.0,
                1.0);

            effectiveOpeningArea += effectiveArea;
            weightedDischargeArea += effectiveArea * dischargeCoefficient;
            weightedOpeningHeight += effectiveArea * Math.Max(opening.OpeningHeightM ?? input.OpeningHeightM, 0.0);
        }

        var effectiveDischargeCoefficient = effectiveOpeningArea > 0.0
            ? weightedDischargeArea / effectiveOpeningArea
            : Math.Clamp(input.DischargeCoefficient, 0.0, 1.0);

        var averageOpeningHeight = effectiveOpeningArea > 0.0
            ? weightedOpeningHeight / effectiveOpeningArea
            : Math.Max(input.OpeningHeightM, 0.0);

        var temperatureDeltaC = Math.Abs(drivingForces.IndoorTemperatureC - drivingForces.OutdoorTemperatureC);
        var averageTemperatureK = Math.Max(
            ((drivingForces.IndoorTemperatureC + CelsiusToKelvin) + (drivingForces.OutdoorTemperatureC + CelsiusToKelvin)) / 2.0,
            MinimumKelvin);

        var usefulHeight = Math.Max(input.UsefulHeightDifferenceM, averageOpeningHeight);
        usefulHeight = Math.Max(usefulHeight, 0.0);

        var stackCoefficient = Math.Max(input.StackCoefficient, 0.0);
        var windCoefficient = Math.Max(input.WindCoefficient, 0.0);
        var windSpeed = Math.Max(drivingForces.WindSpeedMPerS, 0.0);
        var windExposure = Math.Max(input.WindExposureFactor, 0.0);
        var windPressureCoefficient = Math.Abs(input.WindPressureCoefficient);
        var singleSidedOpeningCoefficient = Math.Max(options.SingleSidedOpeningCoefficient, 0.0);

        var stackAirflowM3PerS = 0.0;
        if (effectiveOpeningArea > 0.0 && temperatureDeltaC > 0.0 && usefulHeight > 0.0 && stackCoefficient > 0.0)
        {
            var stackVelocity =
                Math.Sqrt(2.0 * GravityMPerS2 * usefulHeight * (temperatureDeltaC / averageTemperatureK));
            stackAirflowM3PerS = effectiveOpeningArea * effectiveDischargeCoefficient * stackCoefficient * stackVelocity * singleSidedOpeningCoefficient;
        }

        var windAirflowM3PerS = 0.0;
        if (effectiveOpeningArea > 0.0 && windSpeed > 0.0 && windCoefficient > 0.0 && windExposure > 0.0 && windPressureCoefficient > 0.0)
        {
            var windVelocityFactor = Math.Sqrt(windPressureCoefficient * windExposure);
            windAirflowM3PerS = effectiveOpeningArea * effectiveDischargeCoefficient * windCoefficient * windSpeed * windVelocityFactor * singleSidedOpeningCoefficient;
        }

        var (selectedAirflowM3PerS, selectedBranch, calculationMode) = SelectBranch(
            options.BranchSelectionMode,
            stackAirflowM3PerS,
            windAirflowM3PerS,
            effectiveOpeningArea);

        var densityCorrectionDiagnostics = new List<Iso16798NaturalVentilationDiagnostics>();
        var correctedDensity = ResolveCorrectedAirDensity(
            drivingForces,
            averageTemperatureK,
            options,
            densityCorrectionDiagnostics);
        diagnostics.AddRange(densityCorrectionDiagnostics);

        var airflowM3PerHour = selectedAirflowM3PerS * 3600.0;
        var ach = input.RoomVolumeM3 > 0.0
            ? airflowM3PerHour / input.RoomVolumeM3
            : 0.0;

        var maxAch = Math.Max(options.MaximumAirChangesPerHour ?? input.MaximumAirChangesPerHour, 0.0);
        var clampedAch = Math.Clamp(ach, 0.0, maxAch);
        string? clampReason = null;
        if (clampedAch < ach)
        {
            clampReason = $"Air-change rate was clamped to maximum ACH {maxAch:F6}.";
            diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                "Iso16798Ventilation.AchClamped",
                clampReason));
        }

        var clampedFlowM3PerS = clampedAch * input.RoomVolumeM3 / 3600.0;
        var airSpecificHeat = drivingForces.AirSpecificHeatJPerKgK ?? input.AirSpecificHeatJPerKgK;
        var heatTransferCoefficient = correctedDensity * airSpecificHeat * clampedFlowM3PerS;

        diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
            "Iso16798Ventilation.CalculationMode",
            $"Calculation mode: {calculationMode}."));
        diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
            "Iso16798Ventilation.SelectedBranch",
            $"Selected branch: {selectedBranch}."));
        diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
            "Iso16798Ventilation.Summary",
            $"Effective area {effectiveOpeningArea:F6} m2, stack {stackAirflowM3PerS:F6} m3/s, wind {windAirflowM3PerS:F6} m3/s, selected {selectedAirflowM3PerS:F6} m3/s."));

        return new Iso16798NaturalVentilationResult(
            CalculationMode: calculationMode,
            EffectiveOpeningAreaM2: Round6(effectiveOpeningArea),
            StackAirflowM3PerS: Round6(stackAirflowM3PerS),
            WindAirflowM3PerS: Round6(windAirflowM3PerS),
            TotalAirflowM3PerS: Round6(selectedAirflowM3PerS),
            TotalAirflowM3PerH: Round6(airflowM3PerHour),
            AirChangesPerHour: Round6(ach),
            ClampedAirChangesPerHour: Round6(clampedAch),
            HeatTransferCoefficientWPerK: Round6(heatTransferCoefficient),
            Diagnostics: diagnostics,
            AirflowM3PerHour: Round6(airflowM3PerHour),
            AirChangeRatePerHour: Round6(clampedAch),
            WindComponentM3PerHour: Round6(windAirflowM3PerS * 3600.0),
            StackComponentM3PerHour: Round6(stackAirflowM3PerS * 3600.0),
            SelectedBranch: selectedBranch,
            ClampReason: clampReason,
            ControlReason: null);
    }

    private static IReadOnlyList<NaturalVentilationOpening> ResolveOpenings(Iso16798NaturalVentilationInput input)
    {
        if (input.NaturalVentilationOpenings is { Count: > 0 })
            return input.NaturalVentilationOpenings;

        return input.Openings
            .Select(opening => new NaturalVentilationOpening(
                OpeningId: opening.OpeningId,
                OpeningAreaM2: opening.OpeningAreaM2,
                OpeningFraction: opening.OpeningRatio,
                IsOpen: opening.IsOpen,
                OpeningHeightM: input.OpeningHeightM,
                DischargeCoefficient: null))
            .ToArray();
    }

    private static NaturalVentilationDrivingForces ResolveDrivingForces(Iso16798NaturalVentilationInput input) =>
        input.DrivingForces ?? new NaturalVentilationDrivingForces(
            IndoorTemperatureC: input.IndoorTemperatureC,
            OutdoorTemperatureC: input.OutdoorTemperatureC,
            WindSpeedMPerS: input.WindSpeedMPerS,
            OpeningHeightM: input.OpeningHeightM,
            AirDensityKgPerM3: input.AirDensityKgPerM3,
            AirSpecificHeatJPerKgK: input.AirSpecificHeatJPerKgK,
            AltitudeMeters: null);

    private static double ResolveScheduleFraction(
        NaturalVentilationOpeningSchedule? schedule,
        ICollection<Iso16798NaturalVentilationDiagnostics> diagnostics)
    {
        if (schedule is null)
            return 1.0;

        var profileValue = schedule.OpeningFraction;
        if (schedule.OpeningFractionProfile is { Count: > 0 })
        {
            var index = Math.Clamp(schedule.HourIndex ?? 0, 0, schedule.OpeningFractionProfile.Count - 1);
            profileValue = schedule.OpeningFractionProfile[index];
        }

        var clamped = Math.Clamp(profileValue, 0.0, 1.0);
        if (!NearlyEqual(clamped, profileValue))
        {
            diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                "Iso16798Ventilation.ScheduleFractionClamped",
                $"Opening schedule fraction was clamped from {profileValue:F6} to {clamped:F6}."));
        }

        if (clamped <= 0.0)
        {
            diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                "Iso16798Ventilation.ScheduleClosed",
                "Opening schedule is closed for this step."));
        }

        return clamped;
    }

    private static bool ResolveOccupancyEnabled(
        NaturalVentilationOccupancyControl? control,
        ICollection<Iso16798NaturalVentilationDiagnostics> diagnostics,
        out string? controlReason)
    {
        controlReason = null;
        if (control is null || !control.Enabled || !control.DisableWhenUnoccupied)
            return true;

        var occupancyFraction = Math.Clamp(control.OccupancyFraction, 0.0, 1.0);
        var minimumFraction = Math.Clamp(control.MinimumOccupancyFractionToEnable, 0.0, 1.0);

        if (occupancyFraction >= minimumFraction)
            return true;

        controlReason = $"Occupancy control closed openings because occupancy fraction {occupancyFraction:F6} is below threshold {minimumFraction:F6}.";
        diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
            "Iso16798Ventilation.OccupancyControlClosed",
            controlReason));
        return false;
    }

    private static (double airflowM3PerS, string selectedBranch, Iso16798NaturalVentilationCalculationMode mode) SelectBranch(
        NaturalVentilationBranchSelectionMode mode,
        double stackAirflowM3PerS,
        double windAirflowM3PerS,
        double effectiveOpeningAreaM2)
    {
        if (effectiveOpeningAreaM2 <= 0.0 || (stackAirflowM3PerS <= 0.0 && windAirflowM3PerS <= 0.0))
            return (0.0, "Closed", Iso16798NaturalVentilationCalculationMode.ClosedOpenings);

        if (mode == NaturalVentilationBranchSelectionMode.MaxOfWindAndStack)
        {
            if (stackAirflowM3PerS >= windAirflowM3PerS)
                return (stackAirflowM3PerS, "MaxWindStack:Stack", Iso16798NaturalVentilationCalculationMode.MaxWindOrStack);

            return (windAirflowM3PerS, "MaxWindStack:Wind", Iso16798NaturalVentilationCalculationMode.MaxWindOrStack);
        }

        if (stackAirflowM3PerS > 0.0 && windAirflowM3PerS > 0.0)
            return (stackAirflowM3PerS + windAirflowM3PerS, "WindStackSum", Iso16798NaturalVentilationCalculationMode.StackAndWind);

        if (stackAirflowM3PerS > 0.0)
            return (stackAirflowM3PerS, "StackOnly", Iso16798NaturalVentilationCalculationMode.StackOnly);

        return (windAirflowM3PerS, "WindOnly", Iso16798NaturalVentilationCalculationMode.WindOnly);
    }

    private static double ResolveCorrectedAirDensity(
        NaturalVentilationDrivingForces forces,
        double averageTemperatureK,
        NaturalVentilationCalculationOptions options,
        ICollection<Iso16798NaturalVentilationDiagnostics> diagnostics)
    {
        var density = forces.AirDensityKgPerM3 ?? SeaLevelReferenceDensityKgPerM3;
        density = Math.Max(density, 0.001);
        var corrected = density;

        if (options.UseDensityCorrection)
        {
            var referenceTemperatureK = 20.0 + CelsiusToKelvin;
            corrected *= referenceTemperatureK / averageTemperatureK;
        }

        if (options.UseAltitudeDensityCorrection && forces.AltitudeMeters.HasValue)
        {
            var altitude = Math.Max(forces.AltitudeMeters.Value, 0.0);
            corrected *= Math.Exp(-altitude / AltitudeScaleHeightM);
            diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
                "Iso16798Ventilation.AltitudeDensityCorrectionApplied",
                $"Altitude density correction applied for altitude {altitude:F2} m."));
        }

        corrected = Math.Max(corrected, 0.001);
        return corrected;
    }

    private static Iso16798NaturalVentilationResult CreateClosedResult(
        List<Iso16798NaturalVentilationDiagnostics> diagnostics,
        string? controlReason,
        string? clampReason)
    {
        diagnostics.Add(new Iso16798NaturalVentilationDiagnostics(
            "Iso16798Ventilation.CalculationMode",
            "Calculation mode: ClosedOpenings."));

        return new Iso16798NaturalVentilationResult(
            CalculationMode: Iso16798NaturalVentilationCalculationMode.ClosedOpenings,
            EffectiveOpeningAreaM2: 0.0,
            StackAirflowM3PerS: 0.0,
            WindAirflowM3PerS: 0.0,
            TotalAirflowM3PerS: 0.0,
            TotalAirflowM3PerH: 0.0,
            AirChangesPerHour: 0.0,
            ClampedAirChangesPerHour: 0.0,
            HeatTransferCoefficientWPerK: 0.0,
            Diagnostics: diagnostics,
            AirflowM3PerHour: 0.0,
            AirChangeRatePerHour: 0.0,
            WindComponentM3PerHour: 0.0,
            StackComponentM3PerHour: 0.0,
            SelectedBranch: "Closed",
            ClampReason: clampReason,
            ControlReason: controlReason);
    }

    private static void ValidateInput(
        Iso16798NaturalVentilationInput input,
        List<Iso16798NaturalVentilationDiagnostics> diagnostics)
    {
        var hasCompatibilityOpenings = input.Openings is { Count: > 0 };
        var hasExtendedOpenings = input.NaturalVentilationOpenings is { Count: > 0 };

        if (!hasCompatibilityOpenings && !hasExtendedOpenings)
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

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static bool NearlyEqual(double left, double right) =>
        Math.Abs(left - right) < 1e-12;
}
