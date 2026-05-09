using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterLossCalculatorTests
{
    private readonly DomesticHotWaterLossCalculator _calculator = new();

    [Fact]
    public void StorageDistributionCirculationLosses_AreDeterministic()
    {
        var useful = Enumerable.Repeat(1.0, 8760).ToArray();
        var definition = CreateDefinition();

        var result = _calculator.Calculate(
            usefulDemandProfileKWh: useful,
            lossDefinition: definition,
            hotWaterSetpointProfileCelsius: Enumerable.Repeat(55.0, 8760).ToArray());

        Assert.Equal(8760, result.StorageLossesProfileKWh.Count);
        Assert.Equal(8760, result.DistributionLossesProfileKWh.Count);
        Assert.Equal(8760, result.CirculationLossesProfileKWh.Count);
        Assert.True(result.StorageLossesProfileKWh.Sum() > 0);
        Assert.True(result.DistributionLossesProfileKWh.Sum() > 0);
        Assert.True(result.CirculationLossesProfileKWh.Sum() > 0);
    }

    [Fact]
    public void RecoveredLosses_DoNotIncreaseSystemLoadLane()
    {
        var useful = Enumerable.Repeat(1.0, 8760).ToArray();
        var definition = CreateDefinition() with
        {
            RecoveredLossFraction = 0.5
        };

        var result = _calculator.Calculate(useful, definition, Enumerable.Repeat(55.0, 8760).ToArray());
        var thermal = result.StorageLossesProfileKWh.Sum() + result.DistributionLossesProfileKWh.Sum() + result.CirculationLossesProfileKWh.Sum();

        Assert.InRange(result.RecoveredLossesProfileKWh.Sum(), 0.0, thermal);
    }

    [Fact]
    public void SystemEnergyOwnLosses_ZeroesThermalLossProfiles()
    {
        var useful = Enumerable.Repeat(1.0, 8760).ToArray();
        var definition = CreateDefinition() with
        {
            LossOwnershipPolicy = DomesticHotWaterLossOwnershipPolicy.SystemEnergyOwnLosses
        };

        var result = _calculator.Calculate(useful, definition, Enumerable.Repeat(55.0, 8760).ToArray());

        Assert.Equal(0.0, result.StorageLossesProfileKWh.Sum(), 6);
        Assert.Equal(0.0, result.DistributionLossesProfileKWh.Sum(), 6);
        Assert.Equal(0.0, result.CirculationLossesProfileKWh.Sum(), 6);
    }

    private static DomesticHotWaterLossDefinition CreateDefinition() =>
        new(
            SystemKind: DomesticHotWaterSystemKind.CirculationLoop,
            StorageVolumeLiters: 200,
            StorageLossCoefficientWPerKelvin: 2.0,
            StorageAmbientTemperatureCelsius: 20.0,
            DistributionPipeLengthMeters: 15.0,
            DistributionLossCoefficientWPerMeterKelvin: 0.3,
            CirculationOperationSchedule: null,
            CirculationOperationFraction: 1.0,
            CirculationLoopLengthMeters: 20.0,
            CirculationLossCoefficientWPerMeterKelvin: 0.25,
            RecoveredLossFraction: 0.2,
            AuxiliaryEnergyProfileKWh: null,
            AuxiliaryEnergyPerStepKWh: 0.05,
            LossOwnershipPolicy: DomesticHotWaterLossOwnershipPolicy.DhwOwnLosses,
            TimeStepHours: 1.0,
            Source: "UnitTest",
            Diagnostics: []);
}
