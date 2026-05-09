using AssistantEngineer.Modules.Calculations.Application.Services.Ground;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundBoundaryFoundationFixtureTests
{
    private readonly GroundTemperatureProfileCalculator _profileCalculator = new();
    private readonly GroundBoundaryHeatTransferCalculator _heatTransferCalculator = new();

    [Fact]
    public void Fixtures_AreDeterministicAndMeetExpectedAnchors()
    {
        var fixtures = GroundBoundaryFoundationFixtureLoader.LoadAll();

        foreach (var fixture in fixtures)
        {
            var profile = _profileCalculator.Calculate(fixture.GroundTemperatureProfileRequest);
            var profileLane = profile.GroundTemperatureProfileCelsius!;
            var zoneLane = fixture.ZoneIndoorTemperatureProfileCelsius;
            var profileLength = Math.Min(profileLane.Count, zoneLane.Count);

            var heatTransfer = _heatTransferCalculator.Calculate(new(
                Boundary: fixture.Boundary,
                ZoneIndoorTemperatureProfileCelsius: zoneLane.Take(profileLength).ToArray(),
                GroundTemperatureProfileCelsius: profileLane.Take(profileLength).ToArray(),
                TimeStepHours: fixture.GroundTemperatureProfileRequest.TimeStepHours));

            if (fixture.Expected.EquivalentGroundHeatTransferCoefficientWPerKelvin is { } expectedH)
            {
                Assert.InRange(
                    heatTransfer.EquivalentGroundHeatTransferCoefficientWPerKelvin,
                    expectedH - 1e-6,
                    expectedH + 1e-6);
            }

            if (fixture.Expected.HeatFlowProfileWatts is { } expectedFlows)
            {
                Assert.Equal(expectedFlows.Count, heatTransfer.HeatFlowProfileWatts.Count);
                for (var index = 0; index < expectedFlows.Count; index++)
                {
                    Assert.InRange(
                        heatTransfer.HeatFlowProfileWatts[index],
                        expectedFlows[index] - 1e-6,
                        expectedFlows[index] + 1e-6);
                }
            }

            if (fixture.Expected.AnnualHeatLossKiloWattHours is { } expectedLoss)
            {
                Assert.InRange(
                    heatTransfer.AnnualHeatLossKiloWattHours,
                    expectedLoss - 1e-6,
                    expectedLoss + 1e-6);
            }

            if (fixture.Expected.AnnualHeatGainKiloWattHours is { } expectedGain)
            {
                Assert.InRange(
                    heatTransfer.AnnualHeatGainKiloWattHours,
                    expectedGain - 1e-6,
                    expectedGain + 1e-6);
            }

            if (fixture.Expected.ExpectedColdestStepIndex is { } coldestStepIndex)
            {
                Assert.Equal(coldestStepIndex, IndexOfMin(profileLane));
            }

            if (fixture.Expected.ExpectedWarmestStepIndex is { } warmestStepIndex)
            {
                Assert.Equal(warmestStepIndex, IndexOfMax(profileLane));
            }

            if (fixture.ExpectedDiagnosticCodes is { Count: > 0 } expectedCodes)
            {
                foreach (var code in expectedCodes)
                {
                    Assert.Contains(heatTransfer.Diagnostics, item => item.Code == code);
                }
            }

            if (fixture.Expected.ExteriorComparisonHeatFlowWatts is { } exteriorExpected &&
                fixture.ExteriorTemperatureProfileCelsius is { Count: > 0 } exteriorProfile)
            {
                var comparison = _heatTransferCalculator.Calculate(new(
                    Boundary: fixture.Boundary,
                    ZoneIndoorTemperatureProfileCelsius: zoneLane.Take(profileLength).ToArray(),
                    GroundTemperatureProfileCelsius: exteriorProfile.Take(profileLength).ToArray(),
                    TimeStepHours: fixture.GroundTemperatureProfileRequest.TimeStepHours));

                Assert.Equal(exteriorExpected.Count, comparison.HeatFlowProfileWatts.Count);
                for (var index = 0; index < exteriorExpected.Count; index++)
                {
                    Assert.InRange(
                        comparison.HeatFlowProfileWatts[index],
                        exteriorExpected[index] - 1e-6,
                        exteriorExpected[index] + 1e-6);
                }
            }
        }
    }

    private static int IndexOfMin(IReadOnlyList<double> values)
    {
        var bestIndex = 0;
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index] < values[bestIndex])
                bestIndex = index;
        }

        return bestIndex;
    }

    private static int IndexOfMax(IReadOnlyList<double> values)
    {
        var bestIndex = 0;
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index] > values[bestIndex])
                bestIndex = index;
        }

        return bestIndex;
    }
}
