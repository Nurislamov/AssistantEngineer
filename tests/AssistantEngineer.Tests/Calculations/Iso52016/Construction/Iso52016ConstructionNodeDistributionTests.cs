using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Construction;

public sealed class Iso52016ConstructionNodeDistributionTests
{
    private readonly Iso52016ConstructionAssemblyCalculator _calculator = new(new Iso52016ConstructionReferenceDataProvider());

    [Fact]
    public void NodeDistribution_AlwaysContainsFiveNodes()
    {
        var assembly = new Iso52016ConstructionAssembly(
            AssemblyId: "test-node-distribution",
            Name: "Node distribution wall",
            BoundaryKind: Iso52016ConstructionBoundaryKind.ExternalWall,
            Layers:
            [
                new Iso52016ConstructionMaterialLayer("l1", "Gypsum", 0.012, 0.25, 800, 1090),
                new Iso52016ConstructionMaterialLayer("l2", "Brick", 0.20, 0.70, 1800, 840),
                new Iso52016ConstructionMaterialLayer("l3", "Plaster", 0.015, 0.70, 1400, 840)
            ]);

        var result = _calculator.Calculate(assembly);

        Assert.Equal(5, result.Nodes.Count);
        Assert.Equal(5, result.NodeDistribution.Nodes.Count);
        Assert.All(result.Nodes, node => Assert.True(node.CapacityJPerM2K >= 0.0));
    }

    [Fact]
    public void NodeDistribution_CapacitySharesSumToOne()
    {
        var fixture = Iso52016ConstructionFixtureLoader.LoadAll().First();
        var result = _calculator.Calculate(fixture.Input);
        var shareSum = result.Nodes.Sum(node => node.CapacityShareFraction);

        Assert.Equal(1.0, shareSum, 6);
    }

    [Fact]
    public void NodeDistribution_NodeCapacitiesSumToEffectiveCapacity()
    {
        var fixture = Iso52016ConstructionFixtureLoader.LoadAll().First();
        var result = _calculator.Calculate(fixture.Input);
        var nodeCapacitySum = result.Nodes.Sum(node => node.CapacityJPerM2K);

        Assert.Equal(result.EffectiveInternalHeatCapacityJPerM2K, nodeCapacitySum, 3);
    }
}
