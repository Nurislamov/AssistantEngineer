using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Benchmarks.Application.Services;

internal sealed class VerificationComparator : IVerificationComparator
{
    private readonly VerificationTolerance _defaultTolerance;
    private readonly TimeProvider _timeProvider;

    public VerificationComparator(IOptions<VerificationTolerance> options, TimeProvider timeProvider)
    {
        _defaultTolerance = options.Value;
        _timeProvider = timeProvider;
    }

    public VerificationReport Compare(
        BuildingCalculationResult ourResult,
        EnergyPlusCalculationSummary epResult)
    {
        return Compare(ourResult, epResult, _defaultTolerance);
    }

    private VerificationReport Compare(
        BuildingCalculationResult ourResult,
        EnergyPlusCalculationSummary epResult,
        VerificationTolerance tolerance)
    {
        var report = new VerificationReport
        {
            BuildingId = ourResult.BuildingId,
            BuildingName = ourResult.BuildingName,
            CalculationMethod = ourResult.CalculationMethod,
            ExecutedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            OurCalculation = ourResult,
            EnergyPlusCalculation = epResult
        };

        report.CoolingMetrics = CompareHourlyProfiles(
            ourResult.HourlyHeatLoadW,
            epResult.HourlyCoolingLoadW,
            tolerance);

        report.HeatingMetrics = CreateUnavailableMetrics(
            "Heating verification is not available in the current benchmark workflow because only cooling calculation output is provided for internal comparison.");

        report.VerdictBreakdown =
        [
            CreateBreakdownItem("Cooling", report.CoolingMetrics),
            CreateBreakdownItem("Heating", report.HeatingMetrics)
        ];

        report.Passed = report.VerdictBreakdown.All(item => item.Passed);
        report.Conclusion = BuildConclusion(report.VerdictBreakdown, report.Passed);

        return report;
    }

    private VerificationMetrics CompareHourlyProfiles(
        IReadOnlyList<double> our,
        IReadOnlyList<double> ep,
        VerificationTolerance tolerance)
    {
        if (our.Count == 0 || ep.Count == 0)
        {
            return new VerificationMetrics
            {
                HasComparableData = false,
                WithinTolerance = false,
                Detail = "Hourly profiles are missing for comparison."
            };
        }

        int length = Math.Min(our.Count, ep.Count);
        double rmse = 0;
        double maxAbsError = 0;
        double ourPeak = our.Max();
        double epPeak = ep.Max();

        for (int i = 0; i < length; i++)
        {
            double error = our[i] - ep[i];
            rmse += error * error;
            maxAbsError = Math.Max(maxAbsError, Math.Abs(error));
        }
        rmse = Math.Sqrt(rmse / length);

        double peakDiffPercent = epPeak > 0 ? Math.Abs(ourPeak - epPeak) / epPeak * 100 : 0;

        return new VerificationMetrics
        {
            Rmse = Math.Round(rmse, 2, MidpointRounding.AwayFromZero),
            MaxAbsoluteError = Math.Round(maxAbsError, 2, MidpointRounding.AwayFromZero),
            PeakLoadDifferencePercent = Math.Round(peakDiffPercent, 2, MidpointRounding.AwayFromZero),
            HasComparableData = true,
            WithinTolerance = rmse <= tolerance.RmseTolerance &&
                              maxAbsError <= tolerance.MaxAbsoluteErrorTolerance &&
                              peakDiffPercent <= tolerance.PeakLoadTolerancePercent
        };
    }

    private static VerificationMetrics CreateUnavailableMetrics(string detail) =>
        new()
        {
            HasComparableData = false,
            WithinTolerance = false,
            Detail = detail
        };

    private static VerificationVerdictBreakdownItem CreateBreakdownItem(
        string component,
        VerificationMetrics metrics) =>
        new()
        {
            Component = component,
            HasComparableData = metrics.HasComparableData,
            Passed = metrics.HasComparableData && metrics.WithinTolerance,
            Status = GetStatus(metrics),
            Detail = metrics.Detail ?? string.Empty
        };

    private static string BuildConclusion(
        IReadOnlyList<VerificationVerdictBreakdownItem> breakdown,
        bool passed)
    {
        var incompleteComponents = breakdown
            .Where(item => !item.HasComparableData)
            .Select(item => item.Component)
            .ToArray();

        if (incompleteComponents.Length > 0)
        {
            return $"Verification is incomplete. Missing comparable benchmark data for: {string.Join(", ", incompleteComponents)}.";
        }

        return passed
            ? "Verification passed. Cooling and heating calculations match EnergyPlus within tolerance."
            : "Verification failed. Deviations exceed tolerance.";
    }

    private static string GetStatus(VerificationMetrics metrics)
    {
        if (!metrics.HasComparableData)
            return "incomplete";

        return metrics.WithinTolerance
            ? "passed"
            : "failed";
    }
}
