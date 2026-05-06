using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;

public sealed class Iso52016ConstructionReferenceDataProvider
{
    public (double InternalM2KPerW, double ExternalM2KPerW) GetDefaultSurfaceResistances(
        Iso52016ConstructionBoundaryKind boundaryKind)
    {
        return boundaryKind switch
        {
            Iso52016ConstructionBoundaryKind.ExternalWall => (0.13, 0.04),
            Iso52016ConstructionBoundaryKind.Roof => (0.10, 0.04),
            Iso52016ConstructionBoundaryKind.Floor => (0.17, 0.04),
            Iso52016ConstructionBoundaryKind.GroundFloor => (0.17, 0.00),
            Iso52016ConstructionBoundaryKind.InternalPartition => (0.13, 0.13),
            Iso52016ConstructionBoundaryKind.AdjacentUnconditioned => (0.13, 0.08),
            _ => (0.13, 0.04)
        };
    }

    public double GetEffectiveCapacityPenetrationResistanceThresholdM2KPerW(
        Iso52016ConstructionBoundaryKind boundaryKind)
    {
        return boundaryKind switch
        {
            Iso52016ConstructionBoundaryKind.Roof => 0.25,
            Iso52016ConstructionBoundaryKind.GroundFloor => 0.45,
            Iso52016ConstructionBoundaryKind.Floor => 0.40,
            _ => 0.35
        };
    }

    public Iso52016ConstructionMassClass ResolveMassClass(double effectiveInternalHeatCapacityJPerM2K)
    {
        if (effectiveInternalHeatCapacityJPerM2K < 50_000.0)
            return Iso52016ConstructionMassClass.VeryLight;
        if (effectiveInternalHeatCapacityJPerM2K < 110_000.0)
            return Iso52016ConstructionMassClass.Light;
        if (effectiveInternalHeatCapacityJPerM2K < 170_000.0)
            return Iso52016ConstructionMassClass.Medium;
        if (effectiveInternalHeatCapacityJPerM2K < 260_000.0)
            return Iso52016ConstructionMassClass.Heavy;
        return Iso52016ConstructionMassClass.VeryHeavy;
    }

    public IReadOnlyList<double> GetFiveNodeCapacityShareFractions(Iso52016ConstructionMassClass massClass)
    {
        return massClass switch
        {
            Iso52016ConstructionMassClass.VeryLight => [0.10, 0.50, 0.25, 0.10, 0.05],
            Iso52016ConstructionMassClass.Light => [0.10, 0.40, 0.30, 0.15, 0.05],
            Iso52016ConstructionMassClass.Medium => [0.10, 0.30, 0.30, 0.20, 0.10],
            Iso52016ConstructionMassClass.Heavy => [0.10, 0.20, 0.30, 0.25, 0.15],
            Iso52016ConstructionMassClass.VeryHeavy => [0.10, 0.15, 0.25, 0.30, 0.20],
            Iso52016ConstructionMassClass.Custom => [0.10, 0.30, 0.30, 0.20, 0.10],
            _ => [0.10, 0.30, 0.30, 0.20, 0.10]
        };
    }
}
