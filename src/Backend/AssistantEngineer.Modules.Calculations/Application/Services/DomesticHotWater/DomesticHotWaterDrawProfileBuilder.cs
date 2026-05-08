using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterDrawProfileBuilder : IDomesticHotWaterDrawProfileBuilder
{
    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    public DomesticHotWaterDrawProfileResult Build(DomesticHotWaterDrawProfileInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Diagnostics);

        if (TryBuildFromAnnual(input, diagnostics, out var annualResult))
            return annualResult;

        if (TryBuildFromDaily(input, diagnostics, out var dailyResult))
            return dailyResult;

        diagnostics.Add(CreateWarning(
            "AE-DHW-DRAW-PROFILE-FLAT-DEFAULTED",
            "No valid draw profile was provided; deterministic flat profile was used."));

        return BuildFlat(diagnostics);
    }

    private static bool TryBuildFromAnnual(
        DomesticHotWaterDrawProfileInput input,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        out DomesticHotWaterDrawProfileResult result)
    {
        result = null!;
        var annual = input.AnnualHourlyFractions8760;
        if (annual is null)
            return false;

        if (annual.Count != 8760 ||
            annual.Any(value => !double.IsFinite(value) || value < 0.0) ||
            annual.Sum() <= 0.0)
        {
            diagnostics.Add(CreateWarning(
                "AE-DHW-DRAW-PROFILE-INVALID-FALLBACK",
                "Provided 8760 annual hourly profile is invalid; fallback flow will be used."));
            return false;
        }

        var annualNormalized = Normalize(annual, targetSum: 1.0);
        var monthlyFractions = DeriveMonthlyFromAnnual(annualNormalized);
        var dailyFractions = DeriveDailyFromAnnual(annualNormalized);

        diagnostics.Add(CreateInfo(
            "AE-DHW-DRAW-PROFILE-BUILT-FROM-8760",
            "Draw profile was built from provided 8760 annual hourly fractions."));
        diagnostics.Add(CreateInfo(
            "AE-DHW-DRAW-PROFILE-NORMALIZED",
            "Draw profile fractions were normalized."));

        result = new DomesticHotWaterDrawProfileResult(
            HourlyFractions24: dailyFractions,
            MonthlyFractions12: monthlyFractions,
            AnnualHourlyFractions8760: annualNormalized,
            Diagnostics: diagnostics.ToArray());
        return true;
    }

    private static bool TryBuildFromDaily(
        DomesticHotWaterDrawProfileInput input,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        out DomesticHotWaterDrawProfileResult result)
    {
        result = null!;
        var daily = input.HourlyFractions24;
        if (daily is null)
            return false;

        if (daily.Count != 24 ||
            daily.Any(value => !double.IsFinite(value) || value < 0.0) ||
            daily.Sum() <= 0.0)
        {
            diagnostics.Add(CreateWarning(
                "AE-DHW-DRAW-PROFILE-INVALID-FALLBACK",
                "Provided 24-hour draw profile is invalid; fallback flow will be used."));
            return false;
        }

        var dailyNormalized = Normalize(daily, targetSum: 1.0);

        IReadOnlyList<double> monthlyNormalized;
        var monthly = input.MonthlyFractions12;
        if (monthly is null)
        {
            monthlyNormalized = Enumerable.Repeat(1.0, 12).ToArray();
        }
        else if (monthly.Count != 12 ||
                 monthly.Any(value => !double.IsFinite(value) || value < 0.0) ||
                 monthly.Sum() <= 0.0)
        {
            diagnostics.Add(CreateWarning(
                "AE-DHW-DRAW-PROFILE-INVALID-FALLBACK",
                "Provided 12-month profile is invalid; fallback flow will be used."));
            return false;
        }
        else
        {
            monthlyNormalized = Normalize(monthly, targetSum: 12.0);
            diagnostics.Add(CreateInfo(
                "AE-DHW-DRAW-PROFILE-MONTHLY-APPLIED",
                "Monthly profile multipliers were applied to the 24-hour shape."));
        }

        var annualRaw = new List<double>(8760);
        for (var month = 0; month < 12; month++)
        {
            var monthlyMultiplier = monthlyNormalized[month];
            for (var day = 0; day < DaysPerMonth[month]; day++)
            {
                for (var hour = 0; hour < 24; hour++)
                {
                    annualRaw.Add(dailyNormalized[hour] * monthlyMultiplier);
                }
            }
        }

        var annualNormalized = Normalize(annualRaw, targetSum: 1.0);

        diagnostics.Add(CreateInfo(
            "AE-DHW-DRAW-PROFILE-BUILT-FROM-24H",
            "8760 annual profile was built by repeating the provided 24-hour shape."));
        diagnostics.Add(CreateInfo(
            "AE-DHW-DRAW-PROFILE-NORMALIZED",
            "Draw profile fractions were normalized."));

        result = new DomesticHotWaterDrawProfileResult(
            HourlyFractions24: dailyNormalized,
            MonthlyFractions12: monthlyNormalized,
            AnnualHourlyFractions8760: annualNormalized,
            Diagnostics: diagnostics.ToArray());
        return true;
    }

    private static DomesticHotWaterDrawProfileResult BuildFlat(
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var hourly24 = Enumerable.Repeat(1.0 / 24.0, 24).ToArray();
        var monthly12 = Enumerable.Repeat(1.0, 12).ToArray();
        var annual8760 = Enumerable.Repeat(1.0 / 8760.0, 8760).ToArray();

        diagnostics.Add(CreateInfo(
            "AE-DHW-DRAW-PROFILE-NORMALIZED",
            "Flat fallback profile is already normalized."));

        return new DomesticHotWaterDrawProfileResult(
            HourlyFractions24: hourly24,
            MonthlyFractions12: monthly12,
            AnnualHourlyFractions8760: annual8760,
            Diagnostics: diagnostics.ToArray());
    }

    private static IReadOnlyList<double> DeriveMonthlyFromAnnual(IReadOnlyList<double> annual)
    {
        var monthlySums = new double[12];
        var offset = 0;
        for (var month = 0; month < 12; month++)
        {
            var hours = DaysPerMonth[month] * 24;
            monthlySums[month] = annual.Skip(offset).Take(hours).Sum();
            offset += hours;
        }

        return Normalize(monthlySums, targetSum: 12.0);
    }

    private static IReadOnlyList<double> DeriveDailyFromAnnual(IReadOnlyList<double> annual)
    {
        var hourTotals = new double[24];
        for (var hourIndex = 0; hourIndex < annual.Count; hourIndex++)
        {
            hourTotals[hourIndex % 24] += annual[hourIndex];
        }

        return Normalize(hourTotals, targetSum: 1.0);
    }

    private static IReadOnlyList<double> Normalize(IEnumerable<double> values, double targetSum)
    {
        var vector = values.ToArray();
        var sum = vector.Sum();
        if (sum <= 0.0)
            return vector;

        var factor = targetSum / sum;
        return vector.Select(value => value * factor).ToArray();
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.ProfileExpansion,
            "DomesticHotWaterDrawProfileBuilder");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.ProfileExpansion,
            "DomesticHotWaterDrawProfileBuilder");
}
