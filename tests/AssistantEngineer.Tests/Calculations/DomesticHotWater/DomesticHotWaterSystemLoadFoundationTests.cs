using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterSystemLoadFoundationTests
{
    private readonly DomesticHotWaterSystemLoadCalculator _calculator = new(
        new DomesticHotWaterSystemLossInputValidator(),
        new DomesticHotWaterStorageLossCalculator(),
        new DomesticHotWaterDistributionLossCalculator(),
        new DomesticHotWaterCirculationLossCalculator(),
        new DomesticHotWaterEn15316HandoffBuilder(),
        new DomesticHotWaterLossCalculator(),
        new StandardCalculationDisclosureFactory());

    [Fact]
    public void SystemLoad_UsesUsefulPlusLossesMinusRecovered()
    {
        var request = new DomesticHotWaterSystemLoadRequest(
            UsefulDemandProfileKWh: Enumerable.Repeat(1.0, 8760).ToArray(),
            LossDefinition: CreateLossDefinition(),
            ColdWaterTemperatureProfileCelsius: null,
            HotWaterSetpointProfileCelsius: Enumerable.Repeat(55.0, 8760).ToArray(),
            TimeStepHours: 1.0);

        var result = _calculator.Calculate(request);

        var expected = result.AnnualSummary.UsefulEnergyKWh +
                       result.AnnualSummary.StorageLossesKWh +
                       result.AnnualSummary.DistributionLossesKWh +
                       result.AnnualSummary.CirculationLossesKWh -
                       result.AnnualSummary.RecoveredLossesKWh;

        Assert.Equal(expected, result.AnnualSummary.SystemLoadKWh, 6);
        Assert.Equal(12, result.MonthlySystemLoadKWh.Count);
    }

    [Fact]
    public void SystemLoad_NeverGoesNegativeAfterRecovery()
    {
        var request = new DomesticHotWaterSystemLoadRequest(
            UsefulDemandProfileKWh: Enumerable.Repeat(0.0, 12).ToArray(),
            LossDefinition: CreateLossDefinition() with
            {
                RecoveredLossFraction = 1.0
            },
            ColdWaterTemperatureProfileCelsius: null,
            HotWaterSetpointProfileCelsius: Enumerable.Repeat(55.0, 12).ToArray(),
            TimeStepHours: 1.0);

        var result = _calculator.Calculate(request);

        Assert.All(result.SystemLoadProfileKWh, value => Assert.True(value >= 0.0));
    }

    private static DomesticHotWaterLossDefinition CreateLossDefinition() =>
        new(
            SystemKind: DomesticHotWaterSystemKind.CirculationLoop,
            StorageVolumeLiters: 180.0,
            StorageLossCoefficientWPerKelvin: 2.0,
            StorageAmbientTemperatureCelsius: 20.0,
            DistributionPipeLengthMeters: 12.0,
            DistributionLossCoefficientWPerMeterKelvin: 0.2,
            CirculationOperationSchedule: null,
            CirculationOperationFraction: 1.0,
            CirculationLoopLengthMeters: 8.0,
            CirculationLossCoefficientWPerMeterKelvin: 0.2,
            RecoveredLossFraction: 0.3,
            AuxiliaryEnergyProfileKWh: null,
            AuxiliaryEnergyPerStepKWh: 0.02,
            LossOwnershipPolicy: DomesticHotWaterLossOwnershipPolicy.DhwOwnLosses,
            TimeStepHours: 1.0,
            Source: "UnitTest",
            Diagnostics: []);
}
