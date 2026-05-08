using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyModuleCalculatorTests
{
    private readonly SystemEnergyModuleCalculator _calculator = new();

    [Fact]
    public void DisabledModulePassesInputThrough()
    {
        var module = CreateModule(SystemEnergyModuleCalculationMode.Disabled);

        var result = _calculator.Calculate(module, SystemEnergyTestData.HourlyConstant(1.0));

        Assert.All(result.HourlyOutputEnergyKWh8760, value => Assert.Equal(1.0, value, 6));
        Assert.All(result.HourlyLossEnergyKWh8760, value => Assert.Equal(0.0, value, 6));
    }

    [Fact]
    public void LossFractionAddsLoss()
    {
        var module = CreateModule(SystemEnergyModuleCalculationMode.LossFraction) with { LossFraction = 0.1 };

        var result = _calculator.Calculate(module, SystemEnergyTestData.HourlyConstant(10.0));

        Assert.Equal(1.0, result.HourlyLossEnergyKWh8760[0], 6);
        Assert.Equal(11.0, result.HourlyOutputEnergyKWh8760[0], 6);
    }

    [Fact]
    public void FixedEfficiencyCalculatesOutputAndLoss()
    {
        var module = CreateModule(SystemEnergyModuleCalculationMode.FixedEfficiency) with
        {
            Efficiency = 0.8,
            LossFraction = null
        };

        var result = _calculator.Calculate(module, SystemEnergyTestData.HourlyConstant(10.0));

        Assert.Equal(12.5, result.HourlyOutputEnergyKWh8760[0], 6);
        Assert.Equal(2.5, result.HourlyLossEnergyKWh8760[0], 6);
    }

    [Fact]
    public void FixedAnnualLossDistributesAcross8760()
    {
        var module = CreateModule(SystemEnergyModuleCalculationMode.FixedLoss) with
        {
            LossFraction = null,
            FixedAnnualLossKWh = 876.0
        };

        var result = _calculator.Calculate(module, SystemEnergyTestData.HourlyConstant(1.0));

        Assert.Equal(0.1, result.HourlyLossEnergyKWh8760[0], 6);
        Assert.Equal(1.1, result.HourlyOutputEnergyKWh8760[0], 6);
    }

    [Fact]
    public void DirectHourlyLossProfileIsUsed()
    {
        var module = CreateModule(SystemEnergyModuleCalculationMode.DirectProfile) with
        {
            LossFraction = null,
            HourlyLossProfileKWh8760 = SystemEnergyTestData.HourlyConstant(0.2)
        };

        var result = _calculator.Calculate(module, SystemEnergyTestData.HourlyConstant(1.0));

        Assert.Equal(0.2, result.HourlyLossEnergyKWh8760[0], 6);
        Assert.Equal(1.2, result.HourlyOutputEnergyKWh8760[0], 6);
    }

    [Fact]
    public void RecoverableLossSplitWorks()
    {
        var module = CreateModule(SystemEnergyModuleCalculationMode.DirectProfile) with
        {
            LossFraction = null,
            HourlyLossProfileKWh8760 = SystemEnergyTestData.HourlyConstant(10.0),
            RecoverableFraction = 0.3
        };

        var result = _calculator.Calculate(module, SystemEnergyTestData.HourlyConstant(1.0));

        Assert.Equal(3.0, result.HourlyRecoverableLossKWh8760[0], 6);
        Assert.Equal(7.0, result.HourlyNonRecoverableLossKWh8760[0], 6);
    }

    [Fact]
    public void UnknownModeDoesNotFallbackSilently()
    {
        var module = CreateModule(SystemEnergyModuleCalculationMode.Unknown);

        var result = _calculator.Calculate(module, SystemEnergyTestData.HourlyConstant(1.0));

        Assert.Equal(1.0, result.HourlyOutputEnergyKWh8760[0], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-MODULE-UNKNOWN-NO-FALLBACK");
    }

    private static SystemEnergyModuleInput CreateModule(SystemEnergyModuleCalculationMode mode) =>
        new(
            ModuleId: "M1",
            ModuleKind: SystemEnergyModuleKind.Distribution,
            EndUse: SystemEnergyEndUse.SpaceHeating,
            CalculationMode: mode,
            LossFraction: 0.1,
            Efficiency: null,
            FixedAnnualLossKWh: null,
            HourlyLossProfileKWh8760: null,
            MonthlyLossProfileKWh: null,
            RecoverableFraction: 0.0,
            RecoveryMode: SystemEnergyRecoveryMode.NonRecoverable,
            Carrier: null,
            Source: "test",
            Diagnostics: []);
}
