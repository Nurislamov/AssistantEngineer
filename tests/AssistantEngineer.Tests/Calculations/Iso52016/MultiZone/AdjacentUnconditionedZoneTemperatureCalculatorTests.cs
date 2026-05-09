using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

namespace AssistantEngineer.Tests.Calculations.Iso52016.MultiZone;

public sealed class AdjacentUnconditionedZoneTemperatureCalculatorTests
{
    private readonly AdjacentUnconditionedZoneTemperatureCalculator _calculator = new();

    [Fact]
    public void CalculatesAdjacentUnconditionedHourlyTemperatureDeterministically()
    {
        var request = new AdjacentUnconditionedZoneTemperatureProfileRequest(
            ConditionId: "ADJ-1",
            ConditionedZoneTemperatureProfileCelsius: [21.0],
            ExteriorTemperatureProfileCelsius: [5.0],
            Mode: AdjacentUnconditionedTemperatureMode.ReductionFactor,
            ReductionFactorB: 0.5);

        var first = _calculator.Calculate(request);
        var second = _calculator.Calculate(request);

        Assert.True(first.IsValid);
        Assert.True(second.IsValid);
        Assert.Equal(first.TemperatureProfileCelsius, second.TemperatureProfileCelsius);
    }

    [Fact]
    public void AdjacentTemperatureStaysBetweenConditionedAndExterior_ForNormalBFactorRange()
    {
        var result = _calculator.Calculate(new AdjacentUnconditionedZoneTemperatureProfileRequest(
            ConditionId: "ADJ-2",
            ConditionedZoneTemperatureProfileCelsius: [22.0],
            ExteriorTemperatureProfileCelsius: [2.0],
            Mode: AdjacentUnconditionedTemperatureMode.ReductionFactor,
            ReductionFactorB: 0.35));

        Assert.True(result.IsValid);
        Assert.Single(result.TemperatureProfileCelsius);
        Assert.InRange(result.TemperatureProfileCelsius[0], 2.0, 22.0);
    }

    [Fact]
    public void DeterministicFallback_EmitsAssumptionsAndDiagnostics()
    {
        var result = _calculator.Calculate(new AdjacentUnconditionedZoneTemperatureProfileRequest(
            ConditionId: "ADJ-FALLBACK",
            ConditionedZoneTemperatureProfileCelsius: [20.0],
            ExteriorTemperatureProfileCelsius: [0.0],
            Mode: AdjacentUnconditionedTemperatureMode.DeterministicFallback,
            FallbackExteriorWeight: 0.6,
            FallbackOffsetCelsius: 1.0));

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Assumptions);
        Assert.Contains(
            result.Diagnostics,
            item => item.Code == "Iso52016.MultiZone.AdjacentUnconditioned.DeterministicFallbackAssumption");
    }
}
