using AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;
using AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

namespace AssistantEngineer.Tests.Calculations.Rollup;

public sealed class EngineeringCalculationModeComparisonEngineTests
{
    private readonly EngineeringCalculationModeComparisonEngine _engine = new();

    [Fact]
    public void Compare_ComputesAbsoluteAndRelativeDeltas()
    {
        var request = new EngineeringCalculationModeComparisonRequest(
            Domain: EngineeringCalculationModeDomain.SystemEnergy,
            CompatibilityModeId: "compat",
            InspiredModeId: "optin",
            CompatibilityMetrics: [new EngineeringCalculationModeMetric("SystemFinalEnergyKWh", 1000)],
            InspiredMetrics: [new EngineeringCalculationModeMetric("SystemFinalEnergyKWh", 1100)],
            AbsoluteTolerances: new Dictionary<string, double> { ["SystemFinalEnergyKWh"] = 150 },
            RelativeTolerancesPercent: new Dictionary<string, double> { ["SystemFinalEnergyKWh"] = 15 });

        var result = _engine.Compare(request);
        var delta = Assert.Single(result.Deltas);

        Assert.Equal(100, delta.AbsoluteDelta, 6);
        Assert.Equal(10, delta.RelativeDeltaPercent!.Value, 6);
        Assert.True(delta.IsPass);
        Assert.Equal("Pass", result.SummaryStatus);
    }

    [Fact]
    public void Compare_HandlesZeroCompatibilityBaselineSafely()
    {
        var request = new EngineeringCalculationModeComparisonRequest(
            Domain: EngineeringCalculationModeDomain.DomesticHotWater,
            CompatibilityModeId: "compat",
            InspiredModeId: "optin",
            CompatibilityMetrics: [new EngineeringCalculationModeMetric("DhwEnergyKWh", 0)],
            InspiredMetrics: [new EngineeringCalculationModeMetric("DhwEnergyKWh", 10)],
            AbsoluteTolerances: new Dictionary<string, double> { ["DhwEnergyKWh"] = 20 },
            RelativeTolerancesPercent: new Dictionary<string, double> { ["DhwEnergyKWh"] = 5 });

        var result = _engine.Compare(request);
        var delta = Assert.Single(result.Deltas);

        Assert.Null(delta.RelativeDeltaPercent);
        Assert.True(delta.IsWarning);
        Assert.True(delta.IsPass);
    }

    [Fact]
    public void Compare_AppliesToleranceAndFailsWhenExceeded()
    {
        var request = new EngineeringCalculationModeComparisonRequest(
            Domain: EngineeringCalculationModeDomain.Ground,
            CompatibilityModeId: "compat",
            InspiredModeId: "optin",
            CompatibilityMetrics: [new EngineeringCalculationModeMetric("GroundHeatTransferWPerK", 20)],
            InspiredMetrics: [new EngineeringCalculationModeMetric("GroundHeatTransferWPerK", 25)],
            AbsoluteTolerances: new Dictionary<string, double> { ["GroundHeatTransferWPerK"] = 1 },
            RelativeTolerancesPercent: new Dictionary<string, double> { ["GroundHeatTransferWPerK"] = 3 });

        var result = _engine.Compare(request);
        var delta = Assert.Single(result.Deltas);

        Assert.False(delta.IsPass);
        Assert.Equal("Fail", result.SummaryStatus);
    }
}
