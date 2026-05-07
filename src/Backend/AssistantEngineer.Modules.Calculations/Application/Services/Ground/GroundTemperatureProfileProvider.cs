using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundTemperatureProfileProvider : IGroundTemperatureProfileProvider
{
    private static readonly int[] DaysInMonthsNonLeap = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private readonly IAnnualProfileShapeValidator _profileShapeValidator;

    public GroundTemperatureProfileProvider(
        IAnnualProfileShapeValidator profileShapeValidator)
    {
        _profileShapeValidator = profileShapeValidator ?? throw new ArgumentNullException(nameof(profileShapeValidator));
    }

    public GroundTemperatureProfileResult BuildProfile(GroundClimateInput climate)
    {
        ArgumentNullException.ThrowIfNull(climate);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(climate.Diagnostics);

        var monthlyOutdoor = ResolveMonthlyOutdoorProfile(climate, diagnostics);
        var annualMeanOutdoor = climate.AnnualMeanOutdoorTemperatureCelsius is { } annualMean && double.IsFinite(annualMean)
            ? annualMean
            : monthlyOutdoor.Average();

        var amplitude = ResolveAmplitude(climate, monthlyOutdoor, diagnostics);
        var phaseShiftDays = ResolvePhaseShiftDays(climate, diagnostics);

        var monthlyGround = BuildMonthlyGroundProfile(
            monthlyOutdoor,
            annualMeanOutdoor,
            amplitude,
            phaseShiftDays);

        var hourlyGround = ExpandMonthlyToHourly(monthlyGround);
        diagnostics.Add(CreateInfo(
            "AE-GROUND-PROFILE-EXPANDED-MONTHLY-TO-HOURLY",
            "Monthly ground boundary temperatures were expanded to a deterministic 8760-hour profile."));

        return new GroundTemperatureProfileResult(
            MonthlyGroundBoundaryTemperaturesCelsius: monthlyGround,
            HourlyGroundBoundaryTemperaturesCelsius: hourlyGround,
            MonthlyOutdoorTemperaturesCelsius: monthlyOutdoor,
            AnnualMeanOutdoorTemperatureCelsius: annualMeanOutdoor,
            GroundTemperatureAmplitudeCelsius: amplitude,
            GroundTemperaturePhaseShiftDays: phaseShiftDays,
            Diagnostics: diagnostics);
    }

    private IReadOnlyList<double> ResolveMonthlyOutdoorProfile(
        GroundClimateInput climate,
        List<StandardCalculationDiagnostic> diagnostics)
    {
        if (climate.HourlyOutdoorTemperaturesCelsius is { Count: > 0 } hourlyProfile)
        {
            var validation = _profileShapeValidator.ValidateHourlyNonLeapProfile(
                hourlyProfile,
                source: "GroundTemperatureProfileProvider.HourlyOutdoorTemperatures");
            diagnostics.AddRange(validation.Diagnostics);

            if (validation.IsValid)
            {
                diagnostics.Add(CreateInfo(
                    "AE-GROUND-CLIMATE-SOURCE-HOURLY",
                    "Ground profile used hourly outdoor temperature source."));
                return BuildMonthlyMeansFromHourly(hourlyProfile);
            }
        }

        if (climate.MonthlyOutdoorTemperaturesCelsius is { Count: > 0 } monthlyProfile)
        {
            var validation = _profileShapeValidator.ValidateMonthlyProfile(
                monthlyProfile,
                source: "GroundTemperatureProfileProvider.MonthlyOutdoorTemperatures");
            diagnostics.AddRange(validation.Diagnostics);

            if (validation.IsValid)
            {
                diagnostics.Add(CreateInfo(
                    "AE-GROUND-CLIMATE-SOURCE-MONTHLY",
                    "Ground profile used monthly outdoor temperature source."));
                return monthlyProfile.ToArray();
            }
        }

        if (climate.AnnualMeanOutdoorTemperatureCelsius is { } annualMean &&
            double.IsFinite(annualMean))
        {
            diagnostics.Add(CreateInfo(
                "AE-GROUND-CLIMATE-SOURCE-ANNUAL-MEAN",
                "Ground profile used annual mean outdoor temperature source."));
            return Enumerable.Repeat(annualMean, 12).ToArray();
        }

        diagnostics.Add(CreateWarning(
            "AE-GROUND-CLIMATE-MISSING",
            "Ground profile input did not provide a valid climate source; fallback annual mean of 10 C was applied."));
        return Enumerable.Repeat(10.0, 12).ToArray();
    }

    private static double ResolveAmplitude(
        GroundClimateInput climate,
        IReadOnlyList<double> monthlyOutdoor,
        List<StandardCalculationDiagnostic> diagnostics)
    {
        if (climate.GroundTemperatureAmplitudeCelsius is { } providedAmplitude &&
            double.IsFinite(providedAmplitude))
        {
            return Math.Abs(providedAmplitude);
        }

        var inferredAmplitude = monthlyOutdoor.Max() - monthlyOutdoor.Min();
        var safeAmplitude = inferredAmplitude <= 0.0 ? 0.0 : inferredAmplitude * 0.3;
        diagnostics.Add(CreateInfo(
            "AE-GROUND-AMPLITUDE-DEFAULTED",
            "Ground temperature amplitude was not provided and was inferred deterministically from the outdoor annual range."));
        return safeAmplitude;
    }

    private static double ResolvePhaseShiftDays(
        GroundClimateInput climate,
        List<StandardCalculationDiagnostic> diagnostics)
    {
        if (climate.GroundTemperaturePhaseShiftDays is { } providedPhaseShift &&
            double.IsFinite(providedPhaseShift))
        {
            return providedPhaseShift;
        }

        diagnostics.Add(CreateInfo(
            "AE-GROUND-PHASE-SHIFT-DEFAULTED",
            "Ground temperature phase shift was not provided and defaulted to 30 days."));
        return 30.0;
    }

    private static IReadOnlyList<double> BuildMonthlyGroundProfile(
        IReadOnlyList<double> monthlyOutdoor,
        double annualMeanOutdoor,
        double amplitude,
        double phaseShiftDays)
    {
        var result = new double[12];
        var dayOffset = 0;

        for (var monthIndex = 0; monthIndex < 12; monthIndex++)
        {
            var monthMidDay = dayOffset + (DaysInMonthsNonLeap[monthIndex] / 2.0);
            var angle = (2.0 * Math.PI * (monthMidDay - phaseShiftDays)) / 365.0;
            var seasonalSignal = annualMeanOutdoor + amplitude * Math.Cos(angle);
            // Blend seasonal signal with monthly outdoor profile to keep deterministic smoothing.
            result[monthIndex] = 0.7 * seasonalSignal + 0.3 * monthlyOutdoor[monthIndex];
            dayOffset += DaysInMonthsNonLeap[monthIndex];
        }

        return result;
    }

    private static IReadOnlyList<double> BuildMonthlyMeansFromHourly(IReadOnlyList<double> hourly)
    {
        var monthly = new double[12];
        var hourCursor = 0;

        for (var monthIndex = 0; monthIndex < 12; monthIndex++)
        {
            var hoursInMonth = DaysInMonthsNonLeap[monthIndex] * 24;
            var sum = 0.0;
            for (var hour = 0; hour < hoursInMonth; hour++)
            {
                sum += hourly[hourCursor + hour];
            }

            monthly[monthIndex] = sum / hoursInMonth;
            hourCursor += hoursInMonth;
        }

        return monthly;
    }

    private static IReadOnlyList<double> ExpandMonthlyToHourly(IReadOnlyList<double> monthlyValues)
    {
        var hourly = new double[8760];
        var cursor = 0;

        for (var monthIndex = 0; monthIndex < 12; monthIndex++)
        {
            var monthValue = monthlyValues[monthIndex];
            var monthHours = DaysInMonthsNonLeap[monthIndex] * 24;
            for (var monthHour = 0; monthHour < monthHours; monthHour++)
            {
                hourly[cursor++] = monthValue;
            }
        }

        return hourly;
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.BoundaryCondition,
            "GroundTemperatureProfileProvider");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.BoundaryCondition,
            "GroundTemperatureProfileProvider");
}
