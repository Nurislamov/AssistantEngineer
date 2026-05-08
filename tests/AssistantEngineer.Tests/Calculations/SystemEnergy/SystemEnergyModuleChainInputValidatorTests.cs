using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyModuleChainInputValidatorTests
{
    private readonly SystemEnergyModuleChainInputValidator _validator = new(new SystemEnergyUsefulLoadValidator());

    [Fact]
    public void AcceptsValidModuleChainInput()
    {
        var input = CreateValidInput();

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsMissingModuleId()
    {
        var module = CreateValidModule() with { ModuleId = string.Empty };
        var input = CreateValidInput(module);

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-MODULE-ID-MISSING");
    }

    [Fact]
    public void RejectsInvalidLossFraction()
    {
        var module = CreateValidModule() with { LossFraction = 1.2 };
        var input = CreateValidInput(module);

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-MODULE-LOSS-FRACTION-INVALID");
    }

    [Fact]
    public void RejectsInvalidEfficiency()
    {
        var module = CreateValidModule() with
        {
            CalculationMode = SystemEnergyModuleCalculationMode.FixedEfficiency,
            Efficiency = 0.0,
            LossFraction = null
        };
        var input = CreateValidInput(module);

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-MODULE-EFFICIENCY-INVALID");
    }

    [Fact]
    public void RejectsInvalidHourlyLossProfile()
    {
        var module = CreateValidModule() with
        {
            CalculationMode = SystemEnergyModuleCalculationMode.DirectProfile,
            LossFraction = null,
            HourlyLossProfileKWh8760 = Enumerable.Repeat(0.1, 8759).ToArray()
        };
        var input = CreateValidInput(module);

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-MODULE-HOURLY-LOSS-PROFILE-INVALID");
    }

    private static SystemEnergyModuleChainInput CreateValidInput(SystemEnergyModuleInput? module = null) =>
        new(
            CalculationId: "CHAIN-1",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(),
            Modules: [module ?? CreateValidModule()],
            DisclosureOverride: null,
            Source: "test");

    private static SystemEnergyModuleInput CreateValidModule() =>
        new(
            ModuleId: "M-DIST",
            ModuleKind: SystemEnergyModuleKind.Distribution,
            EndUse: SystemEnergyEndUse.SpaceHeating,
            CalculationMode: SystemEnergyModuleCalculationMode.LossFraction,
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
