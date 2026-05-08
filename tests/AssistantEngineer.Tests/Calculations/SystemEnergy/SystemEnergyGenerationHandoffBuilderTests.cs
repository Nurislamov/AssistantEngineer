using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyGenerationHandoffBuilderTests
{
    [Fact]
    public void PreservesEndUseHourlyProfiles()
    {
        var result = BuildSampleChainResult();
        var builder = new SystemEnergyGenerationHandoffBuilder();

        var handoff = builder.Build(result);

        Assert.True(handoff.HourlySystemLoadBeforeGenerationByEndUseKWh8760.ContainsKey(SystemEnergyEndUse.SpaceHeating));
        Assert.True(handoff.HourlySystemLoadBeforeGenerationByEndUseKWh8760.ContainsKey(SystemEnergyEndUse.DomesticHotWater));
    }

    [Fact]
    public void HandoffIsClearlyMarkedAsHandoffOnly()
    {
        var result = BuildSampleChainResult();
        var builder = new SystemEnergyGenerationHandoffBuilder();

        var handoff = builder.Build(result);

        Assert.Contains(handoff.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GENERATION-HANDOFF-ONLY");
    }

    private static SystemEnergyModuleChainResult BuildSampleChainResult()
    {
        var calculator = new SystemEnergyModuleChainCalculator(
            new SystemEnergyModuleChainInputValidator(new SystemEnergyUsefulLoadValidator()),
            new SystemEnergyModuleCalculator(),
            new SystemEnergyGenerationHandoffBuilder(),
            new StandardCalculationDisclosureFactory());

        var heating = SystemEnergyTestData.CreateUsefulLoad(loadId: "H", endUse: SystemEnergyEndUse.SpaceHeating, hourlyValue: 1.0);
        var dhw = SystemEnergyTestData.CreateUsefulLoad(loadId: "D", endUse: SystemEnergyEndUse.DomesticHotWater, hourlyValue: 2.0);
        var input = new SystemEnergyModuleChainInput(
            CalculationId: "CHAIN-HANDOFF",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(usefulLoads: [heating, dhw]),
            Modules: [],
            DisclosureOverride: null,
            Source: "test");

        return calculator.Calculate(input);
    }
}
