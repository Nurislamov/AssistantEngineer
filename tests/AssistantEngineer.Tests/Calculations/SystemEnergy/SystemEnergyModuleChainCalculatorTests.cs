using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyModuleChainCalculatorTests
{
    [Fact]
    public void CalculatesSingleEndUseModuleChain()
    {
        var calculator = CreateCalculator();
        var input = new SystemEnergyModuleChainInput(
            CalculationId: "CHAIN-1",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(
                usefulLoads: [SystemEnergyTestData.CreateUsefulLoad(hourlyValue: 1.0)]),
            Modules:
            [
                CreateModule("M-EM", SystemEnergyModuleKind.Emission, SystemEnergyModuleCalculationMode.LossFraction, 0.1),
                CreateModule("M-DIST", SystemEnergyModuleKind.Distribution, SystemEnergyModuleCalculationMode.LossFraction, 0.2)
            ],
            DisclosureOverride: null,
            Source: "test");

        var result = calculator.Calculate(input);
        var endUse = Assert.Single(result.EndUses);

        Assert.Equal(1.32, endUse.HourlySystemLoadBeforeGenerationKWh8760[0], 6);
    }

    [Fact]
    public void GroupsMultipleUsefulLoadsByEndUse()
    {
        var calculator = CreateCalculator();
        var useful1 = SystemEnergyTestData.CreateUsefulLoad(loadId: "L1", hourlyValue: 1.0);
        var useful2 = SystemEnergyTestData.CreateUsefulLoad(loadId: "L2", hourlyValue: 2.0);
        var input = new SystemEnergyModuleChainInput(
            CalculationId: "CHAIN-2",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(usefulLoads: [useful1, useful2]),
            Modules: [],
            DisclosureOverride: null,
            Source: "test");

        var result = calculator.Calculate(input);
        var endUse = Assert.Single(result.EndUses);

        Assert.Equal(3.0, endUse.HourlyUsefulEnergyKWh8760[0], 6);
    }

    [Fact]
    public void SeparatesEndUses()
    {
        var calculator = CreateCalculator();
        var heating = SystemEnergyTestData.CreateUsefulLoad(loadId: "H", endUse: SystemEnergyEndUse.SpaceHeating, hourlyValue: 1.0);
        var dhw = SystemEnergyTestData.CreateUsefulLoad(loadId: "D", endUse: SystemEnergyEndUse.DomesticHotWater, hourlyValue: 2.0);
        var input = new SystemEnergyModuleChainInput(
            CalculationId: "CHAIN-3",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(usefulLoads: [heating, dhw]),
            Modules: [],
            DisclosureOverride: null,
            Source: "test");

        var result = calculator.Calculate(input);

        Assert.Equal(2, result.EndUses.Count);
    }

    [Fact]
    public void GenerationModuleIsDeferred()
    {
        var calculator = CreateCalculator();
        var input = new SystemEnergyModuleChainInput(
            CalculationId: "CHAIN-4",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(),
            Modules:
            [
                CreateModule("M-GEN", SystemEnergyModuleKind.Generation, SystemEnergyModuleCalculationMode.FixedEfficiency, lossFraction: null, efficiency: 0.8)
            ],
            DisclosureOverride: null,
            Source: "test");

        var result = calculator.Calculate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GENERATION-MODULE-DEFERRED");
        Assert.Equal(1.0, result.EndUses.Single().HourlySystemLoadBeforeGenerationKWh8760[0], 6);
    }

    [Fact]
    public void AggregatesBuildingTotals()
    {
        var calculator = CreateCalculator();
        var heating = SystemEnergyTestData.CreateUsefulLoad(loadId: "H", endUse: SystemEnergyEndUse.SpaceHeating, hourlyValue: 1.0);
        var dhw = SystemEnergyTestData.CreateUsefulLoad(loadId: "D", endUse: SystemEnergyEndUse.DomesticHotWater, hourlyValue: 2.0);
        var input = new SystemEnergyModuleChainInput(
            CalculationId: "CHAIN-5",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(usefulLoads: [heating, dhw]),
            Modules: [],
            DisclosureOverride: null,
            Source: "test");

        var result = calculator.Calculate(input);

        Assert.Equal(3.0 * 8760, result.AnnualTotalUsefulEnergyKWh, 6);
        Assert.Equal(result.AnnualTotalUsefulEnergyKWh, result.AnnualTotalSystemLoadBeforeGenerationKWh, 6);
    }

    [Fact]
    public void TracksAuxiliaryEnergySeparately()
    {
        var calculator = CreateCalculator();
        var auxiliary = new SystemEnergyAuxiliaryLoadInput(
            AuxiliaryId: "A1",
            BuildingId: "B1",
            ZoneId: "Z1",
            RoomId: "R1",
            EndUse: SystemEnergyEndUse.Auxiliary,
            Carrier: SystemEnergyCarrier.Electricity,
            HourlyAuxiliaryEnergyKWh8760: SystemEnergyTestData.HourlyConstant(0.1),
            Source: "test",
            Diagnostics: []);
        var input = new SystemEnergyModuleChainInput(
            CalculationId: "CHAIN-6",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(auxiliaryLoads: [auxiliary]),
            Modules: [],
            DisclosureOverride: null,
            Source: "test");

        var result = calculator.Calculate(input);

        Assert.Equal(876.0, result.AnnualTotalAuxiliaryEnergyKWh, 6);
    }

    [Fact]
    public void BuildsGenerationHandoff()
    {
        var calculator = CreateCalculator();
        var input = new SystemEnergyModuleChainInput(
            CalculationId: "CHAIN-7",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(),
            Modules: [],
            DisclosureOverride: null,
            Source: "test");

        var result = calculator.Calculate(input);

        Assert.True(result.GenerationHandoff.HourlySystemLoadBeforeGenerationByEndUseKWh8760.ContainsKey(SystemEnergyEndUse.SpaceHeating));
        Assert.Contains(result.GenerationHandoff.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GENERATION-HANDOFF-ONLY");
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var calculator = CreateCalculator();
        var input = new SystemEnergyModuleChainInput(
            CalculationId: "CHAIN-8",
            UsefulLoadSet: SystemEnergyTestData.CreateUsefulLoadSet(),
            Modules: [],
            DisclosureOverride: null,
            Source: "test");

        var result = calculator.Calculate(input);

        var boundary = result.Disclosure.ClaimBoundary;
        Assert.Contains("Full ISO compliance", boundary.ForbiddenClaims);
        Assert.Contains("Full EN compliance", boundary.ForbiddenClaims);
        Assert.Contains("StandardReference equivalence", boundary.ForbiddenClaims);
        Assert.Contains("EnergyPlus comparison workflow", boundary.ForbiddenClaims);
        Assert.Contains("ASHRAE 140 / BESTEST-style validation anchor", boundary.ForbiddenClaims);
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("Full ISO compliance", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("Full EN compliance", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("StandardReference equivalence", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("EnergyPlus comparison workflow", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("ASHRAE 140 / BESTEST-style validation anchor", StringComparison.Ordinal));
    }

    private static SystemEnergyModuleChainCalculator CreateCalculator() =>
        new(
            new SystemEnergyModuleChainInputValidator(new SystemEnergyUsefulLoadValidator()),
            new SystemEnergyModuleCalculator(),
            new SystemEnergyGenerationHandoffBuilder(),
            new StandardCalculationDisclosureFactory());

    private static SystemEnergyModuleInput CreateModule(
        string moduleId,
        SystemEnergyModuleKind moduleKind,
        SystemEnergyModuleCalculationMode mode,
        double? lossFraction = null,
        double? efficiency = null) =>
        new(
            ModuleId: moduleId,
            ModuleKind: moduleKind,
            EndUse: SystemEnergyEndUse.SpaceHeating,
            CalculationMode: mode,
            LossFraction: lossFraction,
            Efficiency: efficiency,
            FixedAnnualLossKWh: null,
            HourlyLossProfileKWh8760: null,
            MonthlyLossProfileKWh: null,
            RecoverableFraction: 0.0,
            RecoveryMode: SystemEnergyRecoveryMode.NonRecoverable,
            Carrier: null,
            Source: "test",
            Diagnostics: []);
}
