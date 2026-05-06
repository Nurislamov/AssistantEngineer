using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

public sealed class Iso13370GroundBoundaryCalculatorTests
{
    private readonly Iso13370GroundBoundaryCalculator _calculator = new(new Iso13370GroundTemperatureProfileCalculator());

    [Fact]
    public void ClosedOpeningsEquivalentCase_SlabOnGroundIsGroundDominant()
    {
        var input = new Iso13370GroundBoundaryInput(
            AreaM2: 40.0,
            ExposedPerimeterM: 20.0,
            GroundConductivityWPerMK: 2.0,
            FloorUValueWPerM2K: 0.25,
            IndoorAnnualMeanTemperatureC: 21.0,
            OutdoorAnnualMeanTemperatureC: 10.0,
            OutdoorMonthlyMeanTemperaturesC: null,
            GroundAnnualMeanTemperatureC: 12.0,
            GroundTemperatureAmplitudeC: 4.0,
            GroundTemperaturePhaseShiftMonths: 1.0,
            HorizontalInsulationWidthM: 0.0,
            PerimeterInsulationDepthM: 0.0,
            BurialDepthM: 0.0,
            WallHeightBelowGradeM: 0.0,
            UnderfloorVentilationAirChangesPerHour: 0.0,
            ContactKind: Iso13370GroundContactKind.SlabOnGround);

        var result = _calculator.Calculate(input);

        Assert.True(result.GroundWeight > result.OutdoorWeight);
        Assert.Equal(0.0, result.IndoorWeight);
        Assert.Equal(12, result.MonthlyBoundaryTemperaturesC.Count);
        Assert.True(result.HeatTransferCoefficientWPerK > 0.0);
    }

    [Fact]
    public void ConditionedBasement_IncludesIndoorWeight()
    {
        var input = new Iso13370GroundBoundaryInput(
            AreaM2: 50.0,
            ExposedPerimeterM: 22.0,
            GroundConductivityWPerMK: 2.0,
            FloorUValueWPerM2K: 0.28,
            IndoorAnnualMeanTemperatureC: 21.0,
            OutdoorAnnualMeanTemperatureC: 9.0,
            OutdoorMonthlyMeanTemperaturesC: Enumerable.Repeat(9.0, 12).ToArray(),
            GroundAnnualMeanTemperatureC: 11.0,
            GroundTemperatureAmplitudeC: 3.0,
            GroundTemperaturePhaseShiftMonths: 2.0,
            HorizontalInsulationWidthM: 1.0,
            PerimeterInsulationDepthM: 0.6,
            BurialDepthM: 1.6,
            WallHeightBelowGradeM: 1.4,
            UnderfloorVentilationAirChangesPerHour: 0.0,
            ContactKind: Iso13370GroundContactKind.ConditionedBasement);

        var result = _calculator.Calculate(input);
        Assert.True(result.IndoorWeight > 0.0);
    }

    [Fact]
    public void VentilatedCrawlSpace_HasHigherOutdoorWeightWithVentilation()
    {
        var lowVent = new Iso13370GroundBoundaryInput(
            AreaM2: 45.0,
            ExposedPerimeterM: 24.0,
            GroundConductivityWPerMK: 2.0,
            FloorUValueWPerM2K: 0.27,
            IndoorAnnualMeanTemperatureC: 20.0,
            OutdoorAnnualMeanTemperatureC: 11.0,
            OutdoorMonthlyMeanTemperaturesC: Enumerable.Repeat(11.0, 12).ToArray(),
            GroundAnnualMeanTemperatureC: 12.0,
            GroundTemperatureAmplitudeC: 4.0,
            GroundTemperaturePhaseShiftMonths: 2.0,
            HorizontalInsulationWidthM: 0.2,
            PerimeterInsulationDepthM: 0.2,
            BurialDepthM: 0.4,
            WallHeightBelowGradeM: 0.3,
            UnderfloorVentilationAirChangesPerHour: 1.0,
            ContactKind: Iso13370GroundContactKind.VentilatedCrawlSpace);

        var highVent = lowVent with { UnderfloorVentilationAirChangesPerHour = 6.0 };

        var lowResult = _calculator.Calculate(lowVent);
        var highResult = _calculator.Calculate(highVent);

        Assert.True(highResult.OutdoorWeight >= lowResult.OutdoorWeight);
        Assert.True(highResult.GroundWeight <= lowResult.GroundWeight);
    }

    [Fact]
    public void InvalidAreaOrPerimeter_AreClampedWithDiagnostics()
    {
        var input = new Iso13370GroundBoundaryInput(
            AreaM2: -10.0,
            ExposedPerimeterM: 0.0,
            GroundConductivityWPerMK: -1.0,
            FloorUValueWPerM2K: -1.0,
            IndoorAnnualMeanTemperatureC: 20.0,
            OutdoorAnnualMeanTemperatureC: 8.0,
            OutdoorMonthlyMeanTemperaturesC: null,
            GroundAnnualMeanTemperatureC: 10.0,
            GroundTemperatureAmplitudeC: 3.0,
            GroundTemperaturePhaseShiftMonths: 1.0,
            HorizontalInsulationWidthM: 0.0,
            PerimeterInsulationDepthM: 0.0,
            BurialDepthM: 0.0,
            WallHeightBelowGradeM: 0.0,
            UnderfloorVentilationAirChangesPerHour: 0.0,
            ContactKind: Iso13370GroundContactKind.SlabOnGround);

        var result = _calculator.Calculate(input);

        Assert.True(result.HeatTransferCoefficientWPerK > 0.0);
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso13370GroundBoundary.AreaClamped");
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso13370GroundBoundary.PerimeterClamped");
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso13370GroundBoundary.ConductivityClamped");
    }
}
