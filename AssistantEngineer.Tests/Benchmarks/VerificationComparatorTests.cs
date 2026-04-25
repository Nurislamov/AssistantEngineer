using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class VerificationComparatorTests
{
    [Fact]
    public void CompareUsesInjectedTimeProvider()
    {
        var fixedTime = new DateTimeOffset(2026, 4, 19, 9, 15, 0, TimeSpan.Zero);
        var comparator = new VerificationComparator(
            Options.Create(new VerificationTolerance()),
            new FixedTimeProvider(fixedTime));

        var report = comparator.Compare(
            new BuildingCalculationResult
            {
                BuildingId = 7,
                BuildingName = "Main",
                CalculationMethod = "Iso52016",
                HourlyHeatLoadW = [1000, 1200, 900]
            },
            new EnergyPlusCalculationSummary
            {
                HourlyCoolingLoadW = [1000, 1200, 900]
            });

        Assert.Equal(fixedTime.UtcDateTime, report.ExecutedAtUtc);
        Assert.Equal(2, report.VerdictBreakdown.Count);
    }

    [Fact]
    public void CompareDoesNotPassWhenHeatingVerificationIsUnavailable()
    {
        var comparator = new VerificationComparator(
            Options.Create(new VerificationTolerance()),
            TimeProvider.System);

        var report = comparator.Compare(
            new BuildingCalculationResult
            {
                BuildingId = 7,
                BuildingName = "Main",
                CalculationMethod = "Iso52016",
                HourlyHeatLoadW = [1000, 1200, 900]
            },
            new EnergyPlusCalculationSummary
            {
                HourlyCoolingLoadW = [1000, 1200, 900],
                HourlyHeatingLoadW = [500, 550, 530]
            });

        Assert.True(report.CoolingMetrics.WithinTolerance);
        Assert.False(report.HeatingMetrics.HasComparableData);
        Assert.False(report.HeatingMetrics.WithinTolerance);
        Assert.False(report.Passed);
        Assert.Collection(
            report.VerdictBreakdown,
            item =>
            {
                Assert.Equal("Cooling", item.Component);
                Assert.True(item.Passed);
                Assert.Equal("passed", item.Status);
            },
            item =>
            {
                Assert.Equal("Heating", item.Component);
                Assert.False(item.Passed);
                Assert.Equal("incomplete", item.Status);
            });
        Assert.Contains("Heating", report.Conclusion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompareMarksCoolingAsIncompleteWhenProfilesAreMissing()
    {
        var comparator = new VerificationComparator(
            Options.Create(new VerificationTolerance()),
            TimeProvider.System);

        var report = comparator.Compare(
            new BuildingCalculationResult
            {
                BuildingId = 7,
                BuildingName = "Main",
                CalculationMethod = "Iso52016"
            },
            new EnergyPlusCalculationSummary());

        Assert.False(report.CoolingMetrics.HasComparableData);
        Assert.False(report.Passed);
        Assert.Contains("Cooling", report.Conclusion, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(report.VerdictBreakdown, item =>
            item.Component == "Cooling" && item.Status == "incomplete");
    }
}
