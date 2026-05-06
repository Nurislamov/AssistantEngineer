using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016ExternalValidationComparisonEngineTests
{
    private readonly Iso52016ExternalValidationComparisonEngine _engine = new();

    [Fact]
    public void Compare_PassesWithinAbsoluteTolerance()
    {
        var expected = BuildExpected(annualHeatingKWh: 1200.0, peakHeatingW: 2300.0, hourlyCount: 8760);
        var actual = BuildExpected(annualHeatingKWh: 1200.35, peakHeatingW: 2300.2, hourlyCount: 8760);
        var tolerance = new Iso52016ExternalValidationTolerance(AbsoluteTolerance: 0.5, RelativeTolerancePercent: 0.01);

        var result = _engine.Compare("fixture-abs-pass", expected, actual, tolerance);

        Assert.True(result.IsSuccess);
        Assert.Equal(Iso52016ExternalValidationStatus.Passed, result.Status);
        Assert.Empty(result.FailedMetrics);
    }

    [Fact]
    public void Compare_PassesWithinRelativeTolerance()
    {
        var expected = BuildExpected(annualHeatingKWh: 2000.0, peakHeatingW: 3000.0, hourlyCount: 8760);
        var actual = BuildExpected(annualHeatingKWh: 2010.0, peakHeatingW: 3012.0, hourlyCount: 8760);
        var tolerance = new Iso52016ExternalValidationTolerance(AbsoluteTolerance: 1.0, RelativeTolerancePercent: 1.0);

        var result = _engine.Compare("fixture-rel-pass", expected, actual, tolerance);

        Assert.True(result.IsSuccess);
        Assert.Equal(Iso52016ExternalValidationStatus.Passed, result.Status);
    }

    [Fact]
    public void Compare_FailsAndReturnsDeltaDiagnostics()
    {
        var expected = BuildExpected(annualHeatingKWh: 1000.0, peakHeatingW: 2100.0, hourlyCount: 8760);
        var actual = BuildExpected(annualHeatingKWh: 1200.0, peakHeatingW: 2600.0, hourlyCount: 8700);
        var tolerance = new Iso52016ExternalValidationTolerance(AbsoluteTolerance: 10.0, RelativeTolerancePercent: 2.0);

        var result = _engine.Compare("fixture-fail", expected, actual, tolerance);

        Assert.False(result.IsSuccess);
        Assert.Equal(Iso52016ExternalValidationStatus.Failed, result.Status);
        Assert.NotEmpty(result.FailedMetrics);

        var annualHeatingDelta = Assert.Single(result.Deltas, delta => delta.MetricName == "AnnualHeatingKWh");
        Assert.Equal(Iso52016ExternalValidationStatus.Failed, annualHeatingDelta.Status);
        Assert.Contains("Out of tolerance", annualHeatingDelta.Diagnostics);
        Assert.True(annualHeatingDelta.AbsoluteDelta > tolerance.AbsoluteTolerance);
    }

    private static Iso52016ExternalValidationExpectedResult BuildExpected(
        double annualHeatingKWh,
        double peakHeatingW,
        int hourlyCount) =>
        new(
            AnnualHeatingKWh: annualHeatingKWh,
            AnnualCoolingKWh: annualHeatingKWh * 0.2,
            PeakHeatingW: peakHeatingW,
            PeakCoolingW: peakHeatingW * 0.4,
            MeanOperativeTemperatureC: 22.0,
            MaxOperativeTemperatureC: 26.0,
            MinOperativeTemperatureC: 19.0,
            HourlyResultCount: hourlyCount,
            MonthlyHeatingKWh: Enumerable.Repeat(annualHeatingKWh / 12.0, 12).ToArray(),
            MonthlyCoolingKWh: Enumerable.Repeat(annualHeatingKWh * 0.2 / 12.0, 12).ToArray());
}
