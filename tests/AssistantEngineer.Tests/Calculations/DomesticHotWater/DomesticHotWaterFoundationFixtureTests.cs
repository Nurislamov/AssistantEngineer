using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterFoundationFixtureTests
{
    private readonly DomesticHotWaterDrawOffProfileBuilder _drawOffBuilder = new();
    private readonly DomesticHotWaterSystemLoadCalculator _systemLoadCalculator = new(
        new DomesticHotWaterSystemLossInputValidator(),
        new DomesticHotWaterStorageLossCalculator(),
        new DomesticHotWaterDistributionLossCalculator(),
        new DomesticHotWaterCirculationLossCalculator(),
        new DomesticHotWaterEn15316HandoffBuilder(),
        new DomesticHotWaterLossCalculator(),
        new StandardCalculationDisclosureFactory());

    [Fact]
    public void Fixtures_AreDeterministicAndMeetExpectedAnchors()
    {
        var fixtures = DomesticHotWaterFoundationFixtureLoader.LoadAll();

        foreach (var fixture in fixtures)
        {
            var drawOff = _drawOffBuilder.Build(new DomesticHotWaterDrawOffProfileRequest(
                DemandDefinition: fixture.DemandDefinition,
                Resolution: fixture.Resolution,
                NumberOfSteps: fixture.NumberOfSteps,
                Schedule: fixture.Schedule,
                NormalizationMode: DomesticHotWaterScheduleNormalizationMode.NormalizeToUnity,
                FallbackProfileMode: DomesticHotWaterFallbackProfileMode.DeterministicByUseKind,
                DiagnosticsMode: DomesticHotWaterDiagnosticsMode.Verbose));

            var systemLoad = _systemLoadCalculator.Calculate(new DomesticHotWaterSystemLoadRequest(
                UsefulDemandProfileKWh: drawOff.UsefulEnergyProfileKWh,
                LossDefinition: fixture.LossDefinition,
                ColdWaterTemperatureProfileCelsius: null,
                HotWaterSetpointProfileCelsius: Enumerable.Repeat(
                    fixture.DemandDefinition.HotWaterSetpointTemperatureCelsius,
                    drawOff.UsefulEnergyProfileKWh.Count).ToArray(),
                TimeStepHours: fixture.DemandDefinition.TimeStepHours));

            if (fixture.Expected.TotalVolumeLiters is { } expectedVolume)
            {
                Assert.InRange(drawOff.TotalVolumeLiters, expectedVolume - 1e-3, expectedVolume + 1e-3);
            }

            if (fixture.Expected.TotalUsefulEnergyKWh is { } expectedUseful)
            {
                Assert.InRange(drawOff.TotalUsefulEnergyKWh, expectedUseful - 1e-3, expectedUseful + 1e-3);
            }

            if (fixture.Expected.AnnualSystemLoadKWh is { } expectedSystemLoad)
            {
                Assert.InRange(
                    systemLoad.AnnualSummary.SystemLoadKWh,
                    expectedSystemLoad - 1e-3,
                    expectedSystemLoad + 1e-3);
            }

            if (fixture.Expected.AnnualAuxiliaryEnergyKWh is { } expectedAux)
            {
                Assert.InRange(
                    systemLoad.AnnualSummary.AuxiliaryEnergyKWh,
                    expectedAux - 1e-3,
                    expectedAux + 1e-3);
            }

            if (fixture.ExpectedDiagnosticCodes is { Count: > 0 } expectedCodes)
            {
                var diagnostics = drawOff.Diagnostics.Concat(systemLoad.Diagnostics).ToArray();
                foreach (var code in expectedCodes)
                {
                    Assert.Contains(diagnostics, item => item.Code == code);
                }
            }
        }
    }
}
