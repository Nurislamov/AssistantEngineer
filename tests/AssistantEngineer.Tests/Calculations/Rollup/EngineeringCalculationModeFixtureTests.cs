using AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;
using AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

namespace AssistantEngineer.Tests.Calculations.Rollup;

public sealed class EngineeringCalculationModeFixtureTests
{
    private readonly EngineeringCalculationModeComparisonEngine _engine = new();
    private readonly EngineeringCalculationModeCatalogProvider _catalogProvider = new();

    [Fact]
    public void ComparisonFixtures_ParseAndPassExpectedDeltas()
    {
        var fixtures = EngineeringCalculationModeFixtureLoader.LoadComparisonFixtures();
        Assert.NotEmpty(fixtures);

        foreach (var fixture in fixtures)
        {
            var result = _engine.Compare(new EngineeringCalculationModeComparisonRequest(
                Domain: fixture.Domain,
                CompatibilityModeId: fixture.CompatibilityModeId,
                InspiredModeId: fixture.InspiredModeId,
                CompatibilityMetrics: fixture.CompatibilityMetrics,
                InspiredMetrics: fixture.InspiredMetrics,
                AbsoluteTolerances: fixture.AbsoluteTolerances,
                RelativeTolerancesPercent: fixture.RelativeTolerancesPercent));

            Assert.Equal(fixture.ExpectedSummaryStatus, result.SummaryStatus);

            foreach (var expected in fixture.ExpectedDeltas)
            {
                var actual = Assert.Single(result.Deltas, delta =>
                    delta.MetricName == expected.MetricName);
                Assert.Equal(expected.AbsoluteDelta, actual.AbsoluteDelta, 6);
            }
        }
    }

    [Fact]
    public void CatalogFixture_MatchesCatalogContent()
    {
        var fixture = EngineeringCalculationModeFixtureLoader.LoadCatalogFixture();
        var catalog = _catalogProvider.GetCatalog();

        var stageIds = catalog
            .SelectMany(mode => mode.Stages.Select(stage => stage.StageId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        foreach (var expectedStageId in fixture.ExpectedStageIds)
        {
            Assert.Contains(expectedStageId, stageIds);
        }

        var optionFlags = catalog
            .Where(mode => !string.IsNullOrWhiteSpace(mode.OptionFlagName))
            .Select(mode => mode.OptionFlagName!)
            .ToArray();
        foreach (var expectedOptionFlag in fixture.ExpectedOptionFlags)
        {
            Assert.Contains(expectedOptionFlag, optionFlags);
        }
    }
}
