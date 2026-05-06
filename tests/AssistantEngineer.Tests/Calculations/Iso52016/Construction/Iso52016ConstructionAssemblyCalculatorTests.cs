using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Construction;

public sealed class Iso52016ConstructionAssemblyCalculatorTests
{
    private readonly Iso52016ConstructionAssemblyCalculator _calculator = new(new Iso52016ConstructionReferenceDataProvider());

    [Fact]
    public void Calculate_ComputesUValueFromLayerResistance()
    {
        var assembly = new Iso52016ConstructionAssembly(
            AssemblyId: "test-u-value",
            Name: "Test wall",
            BoundaryKind: Iso52016ConstructionBoundaryKind.ExternalWall,
            Layers:
            [
                new Iso52016ConstructionMaterialLayer(
                    LayerId: "layer-1",
                    Name: "Test layer",
                    ThicknessM: 0.2,
                    ConductivityWPerMK: 0.5,
                    DensityKgPerM3: 1000,
                    SpecificHeatJPerKgK: 900)
            ]);

        var result = _calculator.Calculate(assembly);

        Assert.Equal(0.57, result.TotalResistanceM2KPerW, 6);
        Assert.Equal(1.754386, result.UValueWPerM2K, 6);
    }

    [Fact]
    public void Calculate_UsesExplicitMasslessResistance()
    {
        var assembly = new Iso52016ConstructionAssembly(
            AssemblyId: "test-massless-resistance",
            Name: "Massless wall",
            BoundaryKind: Iso52016ConstructionBoundaryKind.ExternalWall,
            Layers:
            [
                new Iso52016ConstructionMaterialLayer(
                    LayerId: "layer-r",
                    Name: "Massless insulation",
                    ThicknessM: 0.01,
                    ConductivityWPerMK: 0.04,
                    DensityKgPerM3: 1.0,
                    SpecificHeatJPerKgK: 1.0,
                    ThermalResistanceM2KPerW: 2.0,
                    IsMassless: true)
            ]);

        var result = _calculator.Calculate(assembly);

        Assert.Equal(2.17, result.TotalResistanceM2KPerW, 6);
        Assert.Equal(0.460829, result.UValueWPerM2K, 6);
        Assert.Equal(0.0, result.ArealHeatCapacityJPerM2K, 6);
    }

    [Fact]
    public void Calculate_InvalidNegativeLayerProperties_AreClampedWithDiagnostics()
    {
        var assembly = new Iso52016ConstructionAssembly(
            AssemblyId: "test-clamped",
            Name: "Clamped wall",
            BoundaryKind: Iso52016ConstructionBoundaryKind.ExternalWall,
            Layers:
            [
                new Iso52016ConstructionMaterialLayer(
                    LayerId: "layer-bad",
                    Name: "Invalid layer",
                    ThicknessM: -0.1,
                    ConductivityWPerMK: -0.3,
                    DensityKgPerM3: -10,
                    SpecificHeatJPerKgK: -20)
            ]);

        var result = _calculator.Calculate(assembly);

        Assert.True(result.TotalResistanceM2KPerW > 0.0);
        Assert.True(result.UValueWPerM2K > 0.0);
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso52016Construction.LayerThicknessClamped");
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso52016Construction.LayerConductivityClamped");
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso52016Construction.LayerDensityClamped");
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso52016Construction.LayerSpecificHeatClamped");
    }
}
