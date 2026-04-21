using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Benchmarks.Application.Services;

public class VerificationComparator : IVerificationComparator
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

        report.Passed = report.CoolingMetrics.WithinTolerance;
        report.Conclusion = report.Passed
            ? "Verification passed. Our calculation matches EnergyPlus within tolerance."
            : "Verification failed. Deviations exceed tolerance.";

        return report;
    }

    private VerificationMetrics CompareHourlyProfiles(
        IReadOnlyList<double> our,
        IReadOnlyList<double> ep,
        VerificationTolerance tolerance)
    {
        if (our.Count == 0 || ep.Count == 0)
        {
            return new VerificationMetrics { WithinTolerance = false };
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
            WithinTolerance = rmse <= tolerance.RmseTolerance &&
                              maxAbsError <= tolerance.MaxAbsoluteErrorTolerance &&
                              peakDiffPercent <= tolerance.PeakLoadTolerancePercent
        };
    }
}
