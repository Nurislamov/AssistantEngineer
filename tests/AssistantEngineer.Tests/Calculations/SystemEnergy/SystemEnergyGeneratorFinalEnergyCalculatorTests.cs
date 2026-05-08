using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyGeneratorFinalEnergyCalculatorTests
{
    private readonly SystemEnergyGeneratorFinalEnergyCalculator _calculator = new();

    [Fact]
    public void BoilerFixedEfficiencyCalculatesFinalEnergy()
    {
        var generator = SystemEnergyTestData.CreateGenerator(efficiency: 0.9);
        var assigned = CreateAssignedLoad(90.0);

        var result = _calculator.Calculate(generator, assigned);

        Assert.Equal(100.0, result.HourlyTotalFinalEnergyKWh8760[0], 6);
    }

    [Fact]
    public void ElectricResistanceDefaultsEfficiencyToOneWhenMissing()
    {
        var generator = SystemEnergyTestData.CreateGenerator(
            kind: SystemEnergyGeneratorKind.ElectricResistance,
            carrier: SystemEnergyCarrier.Electricity,
            efficiency: null);
        var assigned = CreateAssignedLoad(10.0);

        var result = _calculator.Calculate(generator, assigned);

        Assert.Equal(10.0, result.HourlyTotalFinalEnergyKWh8760[0], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-ELECTRIC-RESISTANCE-EFFICIENCY-DEFAULTED");
    }

    [Fact]
    public void HeatPumpFixedCopCalculatesElectricity()
    {
        var generator = SystemEnergyTestData.CreateGenerator(
            kind: SystemEnergyGeneratorKind.HeatPump,
            mode: SystemEnergyGeneratorCalculationMode.FixedCop,
            carrier: SystemEnergyCarrier.Electricity,
            efficiency: null,
            cop: 3.0);
        var assigned = CreateAssignedLoad(12.0);

        var result = _calculator.Calculate(generator, assigned);

        Assert.Equal(4.0, result.HourlyTotalFinalEnergyKWh8760[0], 6);
    }

    [Fact]
    public void ChillerFixedEerCalculatesElectricity()
    {
        var generator = SystemEnergyTestData.CreateGenerator(
            kind: SystemEnergyGeneratorKind.Chiller,
            mode: SystemEnergyGeneratorCalculationMode.FixedEer,
            carrier: SystemEnergyCarrier.Electricity,
            efficiency: null,
            eer: 3.0,
            servedEndUses: [SystemEnergyEndUse.SpaceCooling]);
        var assigned = CreateAssignedLoad(15.0, SystemEnergyEndUse.SpaceCooling);

        var result = _calculator.Calculate(generator, assigned);

        Assert.Equal(5.0, result.HourlyTotalFinalEnergyKWh8760[0], 6);
    }

    [Fact]
    public void DistrictHeatingHandoffUsesLoadAsFinalEnergy()
    {
        var generator = SystemEnergyTestData.CreateGenerator(
            kind: SystemEnergyGeneratorKind.DistrictHeating,
            mode: SystemEnergyGeneratorCalculationMode.DistrictHandoff,
            carrier: SystemEnergyCarrier.DistrictHeating,
            efficiency: null);
        var assigned = CreateAssignedLoad(20.0);

        var result = _calculator.Calculate(generator, assigned);

        Assert.Equal(20.0, result.HourlyTotalFinalEnergyKWh8760[0], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-DISTRICT-HANDOFF-USED");
    }

    [Fact]
    public void DirectFinalEnergyProfileIsUsed()
    {
        var generator = SystemEnergyTestData.CreateGenerator(
            mode: SystemEnergyGeneratorCalculationMode.DirectFinalEnergyProfile,
            efficiency: null) with
        {
            HourlyFinalEnergyProfileKWh8760 = SystemEnergyTestData.HourlyConstant(2.0)
        };
        var assigned = CreateAssignedLoad(10.0);

        var result = _calculator.Calculate(generator, assigned);

        Assert.Equal(2.0, result.HourlyTotalFinalEnergyKWh8760[0], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-DIRECT-FINAL-PROFILE-USED");
    }

    [Fact]
    public void AuxiliaryElectricityFractionCalculated()
    {
        var generator = SystemEnergyTestData.CreateGenerator(efficiency: 1.0) with
        {
            AuxiliaryElectricityFraction = 0.05
        };
        var assigned = CreateAssignedLoad(10.0);

        var result = _calculator.Calculate(generator, assigned);

        Assert.Equal(0.5, result.HourlyTotalAuxiliaryElectricityKWh8760[0], 6);
    }

    [Fact]
    public void DisabledGeneratorLeavesLoadUnmet()
    {
        var generator = SystemEnergyTestData.CreateGenerator(mode: SystemEnergyGeneratorCalculationMode.Disabled, efficiency: null);
        var assigned = CreateAssignedLoad(10.0);

        var result = _calculator.Calculate(generator, assigned);

        Assert.Equal(0.0, result.HourlyTotalFinalEnergyKWh8760[0], 6);
        Assert.Equal(SystemEnergyFinalEnergyStatus.Disabled, result.HourlyDispatch[0].Status);
        Assert.Equal(10.0, result.HourlyDispatch[0].UnmetSystemLoadKWh, 6);
    }

    [Fact]
    public void UnknownModeDoesNotFallbackSilently()
    {
        var generator = SystemEnergyTestData.CreateGenerator(mode: SystemEnergyGeneratorCalculationMode.Unknown, efficiency: null);
        var assigned = CreateAssignedLoad(10.0);

        var result = _calculator.Calculate(generator, assigned);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-UNKNOWN-NO-FALLBACK");
    }

    private static SystemEnergyGeneratorAssignedLoad CreateAssignedLoad(
        double hourlyLoad,
        SystemEnergyEndUse endUse = SystemEnergyEndUse.SpaceHeating) =>
        new(
            GeneratorId: "G1",
            HourlyAssignedLoadByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>
            {
                [endUse] = SystemEnergyTestData.HourlyConstant(hourlyLoad)
            },
            Diagnostics: []);
}
