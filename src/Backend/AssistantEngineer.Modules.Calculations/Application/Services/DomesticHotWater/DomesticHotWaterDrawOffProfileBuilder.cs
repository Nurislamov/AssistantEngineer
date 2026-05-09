using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterDrawOffProfileBuilder : IDomesticHotWaterDrawOffProfileBuilder
{
    private const double WaterDensityKgPerLiter = 0.997;
    private const double WaterSpecificHeatJPerKgKelvin = 4186.0;

    public DomesticHotWaterDrawOffProfileResult Build(DomesticHotWaterDrawOffProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.DemandDefinition);

        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(request.DemandDefinition.Diagnostics);

        var stepCount = Math.Max(0, request.NumberOfSteps);
        if (stepCount <= 0)
        {
            warnings.Add("Requested profile step count was non-positive and defaulted to 1.");
            diagnostics.Add(CreateWarning(
                "AE-DHW-DRAWOFF-STEPS-DEFAULTED",
                "Draw-off profile step count must be positive; 1-step fallback was used."));
            stepCount = 1;
        }

        var deltaT = request.DemandDefinition.HotWaterSetpointTemperatureCelsius - request.DemandDefinition.ColdWaterTemperatureCelsius;
        if (!double.IsFinite(deltaT) || deltaT <= 0.0)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-DRAWOFF-TEMPERATURE-RISE-NONPOSITIVE",
                "Hot water setpoint temperature must be greater than cold water temperature."));
            deltaT = 0.0;
        }

        var schedule = ResolveSchedule(request, stepCount, warnings, diagnostics);
        var normalizedSchedule = request.NormalizationMode == DomesticHotWaterScheduleNormalizationMode.NormalizeToUnity
            ? NormalizeToUnity(schedule, warnings, diagnostics)
            : schedule;

        var totalVolume = ResolveTotalVolume(request.DemandDefinition, stepCount, normalizedSchedule, deltaT, assumptions, warnings, diagnostics);
        var totalUsefulEnergy = ResolveTotalUsefulEnergy(request.DemandDefinition, totalVolume, deltaT, assumptions, warnings, diagnostics);

        var volumeProfile = BuildVolumeProfile(
            request.DemandDefinition,
            normalizedSchedule,
            totalVolume,
            deltaT,
            warnings,
            diagnostics);
        var usefulProfile = BuildUsefulProfile(
            request.DemandDefinition,
            volumeProfile,
            totalUsefulEnergy,
            deltaT,
            warnings,
            diagnostics);

        assumptions.Add("Useful DHW energy conversion uses Energy_kWh = liters * rho * cp * deltaT / 3,600,000.");
        assumptions.Add("Positive energy values represent useful DHW thermal demand.");

        if (request.DiagnosticsMode == DomesticHotWaterDiagnosticsMode.Minimal)
        {
            diagnostics = diagnostics
                .Where(item => item.Severity != CalculationDiagnosticSeverity.Info)
                .ToList();
        }

        return new DomesticHotWaterDrawOffProfileResult(
            VolumeProfileLiters: volumeProfile,
            UsefulEnergyProfileKWh: usefulProfile,
            TotalVolumeLiters: volumeProfile.Sum(),
            TotalUsefulEnergyKWh: usefulProfile.Sum(),
            Assumptions: assumptions.ToArray(),
            Warnings: warnings.ToArray(),
            Diagnostics: SortDiagnostics(diagnostics));
    }

    private static IReadOnlyList<double> ResolveSchedule(
        DomesticHotWaterDrawOffProfileRequest request,
        int stepCount,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var schedule = request.Schedule;
        if (schedule is not null)
        {
            if (schedule.Count != stepCount)
            {
                diagnostics.Add(CreateError(
                    "AE-DHW-DRAWOFF-SCHEDULE-LENGTH-MISMATCH",
                    $"Schedule length ({schedule.Count}) does not match requested steps ({stepCount})."));
            }
            else if (schedule.Any(value => !double.IsFinite(value) || value < 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-DHW-DRAWOFF-SCHEDULE-NEGATIVE",
                    "Schedule values must be finite and non-negative."));
            }
            else
            {
                return schedule.ToArray();
            }
        }

        var fallbackSchedule = BuildDeterministicFallbackSchedule(request.DemandDefinition.UseKind, request.Resolution, stepCount);
        warnings.Add("Deterministic fallback draw-off profile was used.");
        diagnostics.Add(CreateWarning(
            "AE-DHW-DRAWOFF-DEFAULT-PROFILE-USED",
            "Provided schedule was missing/invalid; deterministic fallback profile was used."));
        return fallbackSchedule;
    }

    private static IReadOnlyList<double> BuildDeterministicFallbackSchedule(
        DomesticHotWaterBuildingUseKind useKind,
        DomesticHotWaterDrawOffProfileResolution resolution,
        int stepCount)
    {
        if (resolution == DomesticHotWaterDrawOffProfileResolution.Monthly)
            return Enumerable.Repeat(1.0, stepCount).ToArray();

        var dayProfile = useKind switch
        {
            DomesticHotWaterBuildingUseKind.Office => new[] { 0.01, 0.01, 0.01, 0.01, 0.01, 0.03, 0.06, 0.08, 0.08, 0.07, 0.06, 0.06, 0.06, 0.06, 0.07, 0.08, 0.08, 0.06, 0.04, 0.03, 0.02, 0.02, 0.01, 0.01 },
            _ => new[] { 0.02, 0.015, 0.015, 0.015, 0.02, 0.03, 0.06, 0.08, 0.08, 0.06, 0.04, 0.035, 0.03, 0.03, 0.03, 0.035, 0.045, 0.06, 0.08, 0.09, 0.08, 0.055, 0.035, 0.025 }
        };

        var normalizedDay = NormalizeToUnity(
            dayProfile,
            new List<string>(),
            new List<StandardCalculationDiagnostic>());
        var schedule = new double[stepCount];
        for (var index = 0; index < stepCount; index++)
            schedule[index] = normalizedDay[index % 24];

        return schedule;
    }

    private static IReadOnlyList<double> NormalizeToUnity(
        IReadOnlyList<double> vector,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var sum = vector.Sum();
        if (sum <= 0.0)
        {
            warnings.Add("All-zero schedule was replaced with flat deterministic schedule.");
            diagnostics.Add(CreateError(
                "AE-DHW-DRAWOFF-SCHEDULE-ALL-ZERO",
                "Schedule sum must be positive."));

            var fallback = Enumerable.Repeat(1.0 / vector.Count, vector.Count).ToArray();
            return fallback;
        }

        return vector.Select(value => value / sum).ToArray();
    }

    private static double ResolveTotalVolume(
        DomesticHotWaterDemandDefinition definition,
        int stepCount,
        IReadOnlyList<double> normalizedSchedule,
        double deltaT,
        ICollection<string> assumptions,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var operatingDays = Math.Clamp(definition.AnnualOperatingDays ?? 365, 0, 366);
        var fixtureCount = Math.Max(0.0, definition.FixtureCount ?? 0.0);

        return definition.DemandBasis switch
        {
            DomesticHotWaterDemandBasis.People or DomesticHotWaterDemandBasis.PerPerson =>
                Math.Max(0.0, definition.OccupantCount ?? 0.0) * Math.Max(0.0, definition.DailyVolumeLitersPerPerson ?? 0.0) * operatingDays,
            DomesticHotWaterDemandBasis.FloorArea or DomesticHotWaterDemandBasis.PerFloorArea =>
                Math.Max(0.0, definition.FloorAreaSquareMeters ?? 0.0) * Math.Max(0.0, definition.DailyVolumeLitersPerSquareMeter ?? 0.0) * operatingDays,
            DomesticHotWaterDemandBasis.DwellingUnit or DomesticHotWaterDemandBasis.PerDwelling =>
                Math.Max(0.0, definition.DwellingCount ?? 0.0) * Math.Max(0.0, definition.DailyVolumeLitersPerDwelling ?? 0.0) * operatingDays,
            DomesticHotWaterDemandBasis.FixtureUse or DomesticHotWaterDemandBasis.PerFixture =>
                fixtureCount * Math.Max(0.0, definition.DailyVolumeLitersPerDwelling ?? definition.DailyVolumeLitersPerPerson ?? 0.0) * operatingDays,
            DomesticHotWaterDemandBasis.CustomDailyVolume =>
                (definition.ScheduledVolumeProfile?.Sum() ?? 0.0) > 0.0
                    ? definition.ScheduledVolumeProfile!.Sum()
                    : Math.Max(0.0, definition.DailyVolumeLitersPerPerson ?? 0.0) * operatingDays,
            DomesticHotWaterDemandBasis.CustomHourlyVolume or DomesticHotWaterDemandBasis.ScheduledVolume =>
                definition.ScheduledVolumeProfile is { Count: > 0 } scheduledVolume
                    ? scheduledVolume.Sum()
                    : Math.Max(0.0, definition.DailyVolumeLitersPerPerson ?? 0.0) * operatingDays,
            DomesticHotWaterDemandBasis.ScheduledEnergy =>
                definition.ScheduledUsefulEnergyProfileKWh is { Count: > 0 } scheduledEnergy && deltaT > 0.0
                    ? scheduledEnergy.Sum() * 3_600_000.0 / (WaterDensityKgPerLiter * WaterSpecificHeatJPerKgKelvin * deltaT)
                    : 0.0,
            _ => 0.0
        };
    }

    private static double ResolveTotalUsefulEnergy(
        DomesticHotWaterDemandDefinition definition,
        double totalVolumeLiters,
        double deltaT,
        ICollection<string> assumptions,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (definition.DemandBasis == DomesticHotWaterDemandBasis.ScheduledEnergy &&
            definition.ScheduledUsefulEnergyProfileKWh is { Count: > 0 } scheduledEnergy)
        {
            assumptions.Add("Scheduled useful energy profile was used directly.");
            return scheduledEnergy.Sum();
        }

        if (deltaT <= 0.0)
            return 0.0;

        return totalVolumeLiters * WaterDensityKgPerLiter * WaterSpecificHeatJPerKgKelvin * deltaT / 3_600_000.0;
    }

    private static IReadOnlyList<double> BuildVolumeProfile(
        DomesticHotWaterDemandDefinition definition,
        IReadOnlyList<double> normalizedSchedule,
        double totalVolumeLiters,
        double deltaT,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (definition.DemandBasis is DomesticHotWaterDemandBasis.CustomHourlyVolume or DomesticHotWaterDemandBasis.ScheduledVolume &&
            definition.ScheduledVolumeProfile is { Count: > 0 } providedVolume)
        {
            if (providedVolume.Any(value => !double.IsFinite(value) || value < 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-DHW-DRAWOFF-SCHEDULE-NEGATIVE",
                    "Scheduled volume profile values must be finite and non-negative."));
            }
            else if (providedVolume.Count == normalizedSchedule.Count)
            {
                return providedVolume.ToArray();
            }
        }

        if (definition.DemandBasis == DomesticHotWaterDemandBasis.ScheduledEnergy &&
            definition.ScheduledUsefulEnergyProfileKWh is { Count: > 0 } energy && deltaT > 0.0)
        {
            if (energy.Count == normalizedSchedule.Count)
            {
                return energy
                    .Select(value => value * 3_600_000.0 / (WaterDensityKgPerLiter * WaterSpecificHeatJPerKgKelvin * deltaT))
                    .ToArray();
            }
        }

        return normalizedSchedule.Select(value => totalVolumeLiters * value).ToArray();
    }

    private static IReadOnlyList<double> BuildUsefulProfile(
        DomesticHotWaterDemandDefinition definition,
        IReadOnlyList<double> volumeProfileLiters,
        double totalUsefulEnergyKWh,
        double deltaT,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (definition.DemandBasis == DomesticHotWaterDemandBasis.ScheduledEnergy &&
            definition.ScheduledUsefulEnergyProfileKWh is { Count: > 0 } energy &&
            energy.Count == volumeProfileLiters.Count &&
            energy.All(value => double.IsFinite(value) && value >= 0.0))
        {
            return energy.ToArray();
        }

        if (deltaT <= 0.0)
            return Enumerable.Repeat(0.0, volumeProfileLiters.Count).ToArray();

        var profile = volumeProfileLiters
            .Select(volume => volume * WaterDensityKgPerLiter * WaterSpecificHeatJPerKgKelvin * deltaT / 3_600_000.0)
            .ToArray();

        if (Math.Abs(profile.Sum() - totalUsefulEnergyKWh) > 1e-6 && totalUsefulEnergyKWh > 0.0)
        {
            var factor = totalUsefulEnergyKWh / profile.Sum();
            for (var index = 0; index < profile.Length; index++)
                profile[index] *= factor;
        }

        return profile;
    }

    private static IReadOnlyList<StandardCalculationDiagnostic> SortDiagnostics(
        IEnumerable<StandardCalculationDiagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.ProfileExpansion,
            "DomesticHotWaterDrawOffProfileBuilder");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.ProfileExpansion,
            "DomesticHotWaterDrawOffProfileBuilder");

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.ProfileExpansion,
            "DomesticHotWaterDrawOffProfileBuilder");
}
