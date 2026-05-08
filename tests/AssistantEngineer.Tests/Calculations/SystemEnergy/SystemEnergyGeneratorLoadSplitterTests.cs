using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyGeneratorLoadSplitterTests
{
    private readonly SystemEnergyGeneratorLoadSplitter _splitter = new();

    [Fact]
    public void SingleGeneratorAssignsFullLoad()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 10.0);
        var generatorSet = SystemEnergyTestData.CreateGeneratorSet(
            [SystemEnergyTestData.CreateGenerator(generatorId: "G1")],
            SystemEnergyLoadSplitMode.SingleGenerator);

        var result = _splitter.SplitLoads(handoff, generatorSet);
        var assigned = Assert.Single(result.AssignedLoads);

        Assert.Equal(10.0, assigned.HourlyAssignedLoadByEndUseKWh8760[SystemEnergyEndUse.SpaceHeating][0], 6);
    }

    [Fact]
    public void FixedFractionSplitsLoad()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 10.0);
        var g1 = SystemEnergyTestData.CreateGenerator(generatorId: "G1", loadFraction: 0.5);
        var g2 = SystemEnergyTestData.CreateGenerator(generatorId: "G2", loadFraction: 0.5);
        var generatorSet = SystemEnergyTestData.CreateGeneratorSet([g1, g2], SystemEnergyLoadSplitMode.FixedFraction);

        var result = _splitter.SplitLoads(handoff, generatorSet);
        var g1Assigned = result.AssignedLoads.Single(load => load.GeneratorId == "G1");
        var g2Assigned = result.AssignedLoads.Single(load => load.GeneratorId == "G2");

        Assert.Equal(5.0, g1Assigned.HourlyAssignedLoadByEndUseKWh8760[SystemEnergyEndUse.SpaceHeating][0], 6);
        Assert.Equal(5.0, g2Assigned.HourlyAssignedLoadByEndUseKWh8760[SystemEnergyEndUse.SpaceHeating][0], 6);
    }

    [Fact]
    public void FixedFractionsNormalizeWhenAboveOne()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 10.0);
        var g1 = SystemEnergyTestData.CreateGenerator(generatorId: "G1", loadFraction: 0.8);
        var g2 = SystemEnergyTestData.CreateGenerator(generatorId: "G2", loadFraction: 0.8);
        var generatorSet = SystemEnergyTestData.CreateGeneratorSet([g1, g2], SystemEnergyLoadSplitMode.FixedFraction);

        var result = _splitter.SplitLoads(handoff, generatorSet);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-FIXED-FRACTIONS-NORMALIZED");
    }

    [Fact]
    public void PriorityOrderUsesFirstGenerator()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 10.0);
        var g1 = SystemEnergyTestData.CreateGenerator(generatorId: "G1", priority: 0);
        var g2 = SystemEnergyTestData.CreateGenerator(generatorId: "G2", priority: 1);
        var generatorSet = SystemEnergyTestData.CreateGeneratorSet([g1, g2], SystemEnergyLoadSplitMode.PriorityOrder);

        var result = _splitter.SplitLoads(handoff, generatorSet);
        var g1Assigned = result.AssignedLoads.Single(load => load.GeneratorId == "G1");
        var g2Assigned = result.AssignedLoads.Single(load => load.GeneratorId == "G2");

        Assert.Equal(10.0, g1Assigned.HourlyAssignedLoadByEndUseKWh8760[SystemEnergyEndUse.SpaceHeating][0], 6);
        Assert.False(g2Assigned.HourlyAssignedLoadByEndUseKWh8760.ContainsKey(SystemEnergyEndUse.SpaceHeating));
    }

    [Fact]
    public void CapacityLimitedPriorityPassesRemainder()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 10.0);
        var g1 = SystemEnergyTestData.CreateGenerator(generatorId: "G1", priority: 0, capacity: 6.0);
        var g2 = SystemEnergyTestData.CreateGenerator(generatorId: "G2", priority: 1, capacity: 10.0);
        var generatorSet = SystemEnergyTestData.CreateGeneratorSet([g1, g2], SystemEnergyLoadSplitMode.CapacityLimitedPriority);

        var result = _splitter.SplitLoads(handoff, generatorSet);
        var g1Assigned = result.AssignedLoads.Single(load => load.GeneratorId == "G1");
        var g2Assigned = result.AssignedLoads.Single(load => load.GeneratorId == "G2");

        Assert.Equal(6.0, g1Assigned.HourlyAssignedLoadByEndUseKWh8760[SystemEnergyEndUse.SpaceHeating][0], 6);
        Assert.Equal(4.0, g2Assigned.HourlyAssignedLoadByEndUseKWh8760[SystemEnergyEndUse.SpaceHeating][0], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-CAPACITY-LIMIT-APPLIED");
    }

    [Fact]
    public void EndUseWithoutGeneratorProducesDiagnostic()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 10.0, endUse: SystemEnergyEndUse.DomesticHotWater);
        var generatorSet = SystemEnergyTestData.CreateGeneratorSet(
            [SystemEnergyTestData.CreateGenerator(servedEndUses: [SystemEnergyEndUse.SpaceHeating])],
            SystemEnergyLoadSplitMode.SingleGenerator);

        var result = _splitter.SplitLoads(handoff, generatorSet);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-ENDUSE-NO-GENERATOR");
    }
}
