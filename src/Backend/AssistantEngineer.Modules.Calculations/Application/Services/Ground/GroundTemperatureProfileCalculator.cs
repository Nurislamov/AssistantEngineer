using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundTemperatureProfileCalculator : IGroundTemperatureProfileCalculator
{
    private static readonly int[] DaysInMonthsNonLeap = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    public GroundTemperatureProfileResult Calculate(GroundTemperatureProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var assumptions = new List<string>();
        var warnings = new List<string>();

        var numberOfSteps = Math.Max(1, request.NumberOfSteps);
        if (request.NumberOfSteps <= 0)
        {
            warnings.Add("NumberOfSteps defaulted to 1 because input was non-positive.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-PROFILE-STEPS-DEFAULTED",
                "Ground temperature profile NumberOfSteps was non-positive and defaulted to 1."));
        }

        var timeStepHours = request.TimeStepHours > 0.0 && double.IsFinite(request.TimeStepHours)
            ? request.TimeStepHours
            : 1.0;
        if (!(request.TimeStepHours > 0.0) || !double.IsFinite(request.TimeStepHours))
        {
            warnings.Add("TimeStepHours defaulted to 1h because input was non-positive or non-finite.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-PROFILE-TIMESTEP-DEFAULTED",
                "Ground temperature profile TimeStepHours was non-positive/non-finite and defaulted to 1h."));
        }

        var phaseShiftDays = ResolvePhaseShiftDays(request, assumptions, warnings, diagnostics);
        var amplitude = ResolveAmplitude(request.GroundTemperatureAmplitudeCelsius, warnings, diagnostics);
        var annualMeanGround = ResolveAnnualMeanGround(request.GroundAnnualMeanTemperatureCelsius, warnings, diagnostics);

        var profile = request.Mode switch
        {
            GroundTemperatureProfileMode.ConstantAnnualMean => BuildConstantProfile(
                annualMeanGround,
                numberOfSteps,
                assumptions,
                diagnostics),
            _ => BuildSeasonalSinusoidalProfile(
                annualMeanGround,
                amplitude,
                phaseShiftDays,
                request.TimeResolution,
                numberOfSteps,
                timeStepHours,
                assumptions,
                warnings,
                diagnostics)
        };

        var monthlyGround = request.TimeResolution == GroundProfileTimeResolution.Monthly
            ? NormalizeMonthlyProfile(profile, diagnostics)
            : BuildMonthlyFromHourly(profile, timeStepHours, diagnostics);
        var hourlyGround = request.TimeResolution == GroundProfileTimeResolution.Hourly
            ? NormalizeHourlyProfile(profile, diagnostics)
            : ExpandMonthlyToHourly(profile, diagnostics);

        var monthlyOutdoor = ResolveMonthlyOutdoor(request, diagnostics);
        var annualMeanOutdoor = request.AnnualMeanOutdoorTemperatureCelsius is { } value && double.IsFinite(value)
            ? value
            : monthlyOutdoor.Average();

        return new GroundTemperatureProfileResult(
            MonthlyGroundBoundaryTemperaturesCelsius: monthlyGround,
            HourlyGroundBoundaryTemperaturesCelsius: hourlyGround,
            MonthlyOutdoorTemperaturesCelsius: monthlyOutdoor,
            AnnualMeanOutdoorTemperatureCelsius: annualMeanOutdoor,
            GroundTemperatureAmplitudeCelsius: amplitude,
            GroundTemperaturePhaseShiftDays: phaseShiftDays,
            Diagnostics: SortDiagnostics(diagnostics),
            GroundTemperatureProfileCelsius: profile,
            Assumptions: assumptions.ToArray(),
            Warnings: warnings.ToArray(),
            Mode: request.Mode,
            TimeResolution: request.TimeResolution);
    }

    private static IReadOnlyList<double> BuildConstantProfile(
        double annualMeanGround,
        int numberOfSteps,
        ICollection<string> assumptions,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        assumptions.Add("ConstantAnnualMean mode: every timestep uses configured annual mean ground temperature.");
        diagnostics.Add(CreateInfo(
            "AE-GROUND-PROFILE-CONSTANT-ANNUAL-MEAN",
            "Ground profile used ConstantAnnualMean mode."));
        return Enumerable.Repeat(annualMeanGround, numberOfSteps).ToArray();
    }

    private static IReadOnlyList<double> BuildSeasonalSinusoidalProfile(
        double annualMeanGround,
        double amplitude,
        double phaseShiftDays,
        GroundProfileTimeResolution timeResolution,
        int numberOfSteps,
        double timeStepHours,
        ICollection<string> assumptions,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        assumptions.Add("SeasonalSinusoidal convention: temperature minimum occurs at phaseShiftDays (coldest day anchor).");
        diagnostics.Add(CreateInfo(
            "AE-GROUND-PROFILE-SEASONAL-SINUSOIDAL",
            "Ground profile used SeasonalSinusoidal mode with deterministic cosine convention."));

        var result = new double[numberOfSteps];
        var annualPeriodHours = 365.0 * 24.0;
        var phaseShiftHours = phaseShiftDays * 24.0;

        for (var step = 0; step < numberOfSteps; step++)
        {
            var elapsedHours = timeResolution == GroundProfileTimeResolution.Monthly
                ? ResolveMonthlyMidpointHour(step)
                : step * timeStepHours;
            var angle = (2.0 * Math.PI * (elapsedHours - phaseShiftHours)) / annualPeriodHours;
            result[step] = annualMeanGround - amplitude * Math.Cos(angle);
        }

        if (timeResolution == GroundProfileTimeResolution.Monthly && numberOfSteps != 12)
        {
            warnings.Add("Monthly mode expected 12 points; profile will be normalized to 12 for compatibility lanes.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-PROFILE-MONTHLY-LENGTH-NONSTANDARD",
                $"Monthly mode produced {numberOfSteps} points; compatibility lanes normalize to 12-month profile."));
        }

        return result;
    }

    private static IReadOnlyList<double> ResolveMonthlyOutdoor(
        GroundTemperatureProfileRequest request,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (request.OutdoorTemperatureProfileCelsius is { Count: 12 } monthly &&
            monthly.All(double.IsFinite))
        {
            return monthly.ToArray();
        }

        if (request.AnnualMeanOutdoorTemperatureCelsius is { } annualMean && double.IsFinite(annualMean))
        {
            diagnostics.Add(CreateInfo(
                "AE-GROUND-PROFILE-OUTDOOR-MONTHLY-DEFAULTED",
                "Monthly outdoor profile defaulted to annual mean outdoor temperature."));
            return Enumerable.Repeat(annualMean, 12).ToArray();
        }

        diagnostics.Add(CreateWarning(
            "AE-GROUND-PROFILE-OUTDOOR-MISSING",
            "Outdoor profile was missing or invalid; monthly outdoor fallback of 10 C was used."));
        return Enumerable.Repeat(10.0, 12).ToArray();
    }

    private static double ResolvePhaseShiftDays(
        GroundTemperatureProfileRequest request,
        ICollection<string> assumptions,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (request.GroundTemperaturePhaseShiftDays is not { } phaseShiftDays || !double.IsFinite(phaseShiftDays))
        {
            assumptions.Add("Phase shift defaulted to 45 days when not provided.");
            diagnostics.Add(CreateInfo(
                "AE-GROUND-PHASE-SHIFT-DEFAULTED",
                "Ground profile phase shift defaulted to 45 days."));
            return 45.0;
        }

        if (phaseShiftDays < 0.0 || phaseShiftDays > 365.0)
        {
            warnings.Add("Phase shift was outside [0,365] and clamped to accepted range.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-PROFILE-PHASE-SHIFT-CLAMPED",
                "Ground profile phase shift was outside [0,365] and was clamped."));
        }

        return Math.Clamp(phaseShiftDays, 0.0, 365.0);
    }

    private static double ResolveAmplitude(
        double amplitudeInput,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!double.IsFinite(amplitudeInput))
        {
            warnings.Add("Amplitude was non-finite and defaulted to 0 C.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-AMPLITUDE-DEFAULTED",
                "Ground temperature amplitude was non-finite and defaulted to 0 C."));
            return 0.0;
        }

        if (amplitudeInput < 0.0)
        {
            warnings.Add("Negative amplitude was converted to absolute value.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-PROFILE-AMPLITUDE-ABS",
                "Ground temperature amplitude was negative and converted to absolute value."));
        }

        return Math.Abs(amplitudeInput);
    }

    private static double ResolveAnnualMeanGround(
        double annualMeanGroundInput,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (double.IsFinite(annualMeanGroundInput))
            return annualMeanGroundInput;

        warnings.Add("Annual mean ground temperature was non-finite and defaulted to 10 C.");
        diagnostics.Add(CreateWarning(
            "AE-GROUND-PROFILE-ANNUAL-MEAN-GROUND-DEFAULTED",
            "Annual mean ground temperature was non-finite and defaulted to 10 C."));
        return 10.0;
    }

    private static IReadOnlyList<double> NormalizeMonthlyProfile(
        IReadOnlyList<double> profile,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (profile.Count == 12)
            return profile.ToArray();

        diagnostics.Add(CreateInfo(
            "AE-GROUND-PROFILE-MONTHLY-NORMALIZED",
            "Monthly profile was normalized to 12 months by deterministic interpolation."));

        var normalized = new double[12];
        for (var month = 0; month < normalized.Length; month++)
        {
            var sourceIndex = (int)Math.Round((profile.Count - 1) * (month / 11.0), MidpointRounding.AwayFromZero);
            normalized[month] = profile[Math.Clamp(sourceIndex, 0, profile.Count - 1)];
        }

        return normalized;
    }

    private static IReadOnlyList<double> NormalizeHourlyProfile(
        IReadOnlyList<double> profile,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (profile.Count == 8760)
            return profile.ToArray();

        diagnostics.Add(CreateInfo(
            "AE-GROUND-PROFILE-HOURLY-NORMALIZED",
            "Hourly profile was normalized to deterministic 8760 non-leap hours."));

        var normalized = new double[8760];
        for (var hour = 0; hour < normalized.Length; hour++)
        {
            var sourceIndex = profile.Count == 1
                ? 0
                : (int)Math.Round((profile.Count - 1) * (hour / 8759.0), MidpointRounding.AwayFromZero);
            normalized[hour] = profile[Math.Clamp(sourceIndex, 0, profile.Count - 1)];
        }

        return normalized;
    }

    private static IReadOnlyList<double> BuildMonthlyFromHourly(
        IReadOnlyList<double> profile,
        double timeStepHours,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var hourly = profile.Count == 8760 && Math.Abs(timeStepHours - 1.0) < 1e-6
            ? profile.ToArray()
            : NormalizeHourlyProfile(profile, diagnostics);

        var monthly = new double[12];
        var cursor = 0;
        for (var month = 0; month < 12; month++)
        {
            var hours = DaysInMonthsNonLeap[month] * 24;
            var sum = 0.0;
            for (var hour = 0; hour < hours; hour++)
            {
                sum += hourly[cursor + hour];
            }

            monthly[month] = sum / hours;
            cursor += hours;
        }

        return monthly;
    }

    private static IReadOnlyList<double> ExpandMonthlyToHourly(
        IReadOnlyList<double> monthlyProfile,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var monthly = NormalizeMonthlyProfile(monthlyProfile, diagnostics);
        var hourly = new double[8760];
        var cursor = 0;
        for (var month = 0; month < 12; month++)
        {
            var hours = DaysInMonthsNonLeap[month] * 24;
            for (var monthHour = 0; monthHour < hours; monthHour++)
            {
                hourly[cursor++] = monthly[month];
            }
        }

        diagnostics.Add(CreateInfo(
            "AE-GROUND-PROFILE-EXPANDED-MONTHLY-TO-HOURLY",
            "Monthly ground profile expanded to deterministic 8760-hour profile."));
        return hourly;
    }

    private static double ResolveMonthlyMidpointHour(int monthIndex)
    {
        var clamped = Math.Clamp(monthIndex, 0, 11);
        var daysBefore = 0;
        for (var month = 0; month < clamped; month++)
        {
            daysBefore += DaysInMonthsNonLeap[month];
        }

        var midDay = daysBefore + DaysInMonthsNonLeap[clamped] / 2.0;
        return midDay * 24.0;
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
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.BoundaryCondition,
            "GroundTemperatureProfileCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.BoundaryCondition,
            "GroundTemperatureProfileCalculator");
}
