using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;

public sealed class Iso52016ConstructionAssemblyCalculator
{
    private readonly Iso52016ConstructionReferenceDataProvider _referenceDataProvider;

    public Iso52016ConstructionAssemblyCalculator(Iso52016ConstructionReferenceDataProvider referenceDataProvider)
    {
        _referenceDataProvider = referenceDataProvider;
    }

    public Iso52016ConstructionAssemblyResult Calculate(Iso52016ConstructionAssembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var layers = assembly.Layers ?? [];
        if (layers.Count == 0)
            throw new ArgumentException("Construction assembly must contain at least one layer.", nameof(assembly));

        var diagnostics = new List<Iso52016ConstructionDiagnostics>();
        var assumptions = new List<string>
        {
            "ISO52016-inspired construction layer and mass class engineering foundation.",
            "Internal deterministic engineering anchors only.",
            "No full ISO 52016 compliance claim.",
            "No StandardReference equivalence claim.",
            "No EnergyPlus comparison workflow claim.",
            "No ASHRAE 140 / BESTEST-style validation anchor claim.",
            "No external certification claim.",
            "Five-node distribution descriptor is a deterministic anchor, not a full transient conduction solver."
        };

        var defaults = _referenceDataProvider.GetDefaultSurfaceResistances(assembly.BoundaryKind);
        var internalSurfaceResistance = ResolveSurfaceResistance(
            assembly.InternalSurfaceResistanceM2KPerW,
            defaults.InternalM2KPerW,
            "Iso52016Construction.InternalSurfaceResistanceDefaulted",
            "Internal surface resistance was defaulted from boundary-kind reference values.",
            diagnostics);
        var externalSurfaceResistance = ResolveSurfaceResistance(
            assembly.ExternalSurfaceResistanceM2KPerW,
            defaults.ExternalM2KPerW,
            "Iso52016Construction.ExternalSurfaceResistanceDefaulted",
            "External surface resistance was defaulted from boundary-kind reference values.",
            diagnostics);

        var layerResults = new List<Iso52016ConstructionLayerResult>(layers.Count);
        var totalLayerResistance = 0.0;
        var totalArealHeatCapacity = 0.0;

        foreach (var layer in layers)
        {
            var resistance = CalculateLayerResistance(layer, diagnostics);
            var arealCapacity = CalculateArealHeatCapacity(layer, diagnostics);
            totalLayerResistance += resistance;
            totalArealHeatCapacity += arealCapacity;

            layerResults.Add(new Iso52016ConstructionLayerResult(
                LayerId: layer.LayerId,
                Name: layer.Name,
                ThicknessM: Round6(layer.ThicknessM),
                ResistanceM2KPerW: Round6(resistance),
                ArealHeatCapacityJPerM2K: Round6(arealCapacity),
                EffectiveInternalCapacityContributionJPerM2K: 0.0,
                IsMassless: layer.IsMassless));
        }

        var totalResistance = Math.Max(0.01, internalSurfaceResistance + totalLayerResistance + externalSurfaceResistance);
        var uValue = 1.0 / totalResistance;

        var penetrationThreshold = _referenceDataProvider
            .GetEffectiveCapacityPenetrationResistanceThresholdM2KPerW(assembly.BoundaryKind);
        var effectiveInternalHeatCapacity = CalculateEffectiveInternalHeatCapacity(
            layerResults,
            penetrationThreshold,
            diagnostics,
            assumptions,
            out var effectiveContributions);

        var finalLayerResults = layerResults
            .Select((item, index) => item with
            {
                EffectiveInternalCapacityContributionJPerM2K = Round6(effectiveContributions[index])
            })
            .ToArray();

        var massClass = _referenceDataProvider.ResolveMassClass(effectiveInternalHeatCapacity);
        var nodes = BuildFiveNodeDescriptor(
            massClass,
            totalResistance,
            effectiveInternalHeatCapacity);
        var distribution = new Iso52016ConstructionNodeDistribution(
            Nodes: nodes,
            TotalCapacityJPerM2K: Round6(effectiveInternalHeatCapacity),
            TotalResistanceM2KPerW: Round6(totalResistance));

        diagnostics.Add(new Iso52016ConstructionDiagnostics(
            "Iso52016Construction.Summary",
            $"Assembly={assembly.AssemblyId}, Boundary={assembly.BoundaryKind}, U={uValue:F6} W/m2K, ArealCapacity={totalArealHeatCapacity:F6} J/m2K, EffectiveCapacity={effectiveInternalHeatCapacity:F6} J/m2K, MassClass={massClass}."));

        return new Iso52016ConstructionAssemblyResult(
            TotalResistanceM2KPerW: Round6(totalResistance),
            UValueWPerM2K: Round6(uValue),
            ArealHeatCapacityJPerM2K: Round6(totalArealHeatCapacity),
            EffectiveInternalHeatCapacityJPerM2K: Round6(effectiveInternalHeatCapacity),
            MassClass: massClass,
            Layers: finalLayerResults,
            Nodes: nodes,
            NodeDistribution: distribution,
            Diagnostics: diagnostics,
            AssumptionsUsed: assumptions);
    }

    private static double CalculateLayerResistance(
        Iso52016ConstructionMaterialLayer layer,
        ICollection<Iso52016ConstructionDiagnostics> diagnostics)
    {
        if (layer.IsMassless && layer.ThermalResistanceM2KPerW is > 0.0)
            return layer.ThermalResistanceM2KPerW.Value;

        var thickness = layer.ThicknessM;
        if (thickness <= 0.0)
        {
            diagnostics.Add(new Iso52016ConstructionDiagnostics(
                "Iso52016Construction.LayerThicknessClamped",
                $"Layer '{layer.LayerId}' thickness was clamped to 0.001 m."));
            thickness = 0.001;
        }

        var conductivity = layer.ConductivityWPerMK;
        if (conductivity <= 0.0)
        {
            diagnostics.Add(new Iso52016ConstructionDiagnostics(
                "Iso52016Construction.LayerConductivityClamped",
                $"Layer '{layer.LayerId}' conductivity was clamped to 0.01 W/mK."));
            conductivity = 0.01;
        }

        return thickness / conductivity;
    }

    private static double CalculateArealHeatCapacity(
        Iso52016ConstructionMaterialLayer layer,
        ICollection<Iso52016ConstructionDiagnostics> diagnostics)
    {
        if (layer.IsMassless)
            return 0.0;

        var thickness = layer.ThicknessM;
        if (thickness <= 0.0)
            thickness = 0.001;

        var density = layer.DensityKgPerM3;
        if (density <= 0.0)
        {
            diagnostics.Add(new Iso52016ConstructionDiagnostics(
                "Iso52016Construction.LayerDensityClamped",
                $"Layer '{layer.LayerId}' density was clamped to 1 kg/m3."));
            density = 1.0;
        }

        var specificHeat = layer.SpecificHeatJPerKgK;
        if (specificHeat <= 0.0)
        {
            diagnostics.Add(new Iso52016ConstructionDiagnostics(
                "Iso52016Construction.LayerSpecificHeatClamped",
                $"Layer '{layer.LayerId}' specific heat was clamped to 1 J/kgK."));
            specificHeat = 1.0;
        }

        return thickness * density * specificHeat;
    }

    private static double CalculateEffectiveInternalHeatCapacity(
        IReadOnlyList<Iso52016ConstructionLayerResult> layers,
        double penetrationThresholdM2KPerW,
        ICollection<Iso52016ConstructionDiagnostics> diagnostics,
        ICollection<string> assumptions,
        out double[] effectiveContributions)
    {
        assumptions.Add(
            $"Effective internal heat capacity uses deterministic resistance-threshold weighting with threshold={penetrationThresholdM2KPerW:F3} m2K/W.");

        var cumulativeResistance = 0.0;
        var effectiveTotal = 0.0;
        effectiveContributions = new double[layers.Count];

        for (var index = 0; index < layers.Count; index++)
        {
            var layer = layers[index];
            var midpointResistance = cumulativeResistance + 0.5 * layer.ResistanceM2KPerW;
            cumulativeResistance += layer.ResistanceM2KPerW;

            var weight = midpointResistance <= penetrationThresholdM2KPerW
                ? 1.0
                : Math.Exp(-(midpointResistance - penetrationThresholdM2KPerW) / Math.Max(0.05, penetrationThresholdM2KPerW));
            weight = Math.Clamp(weight, 0.0, 1.0);

            var effectiveContribution = layer.ArealHeatCapacityJPerM2K * weight;
            effectiveContributions[index] = effectiveContribution;
            effectiveTotal += effectiveContribution;
        }

        if (effectiveTotal <= 0.0)
        {
            diagnostics.Add(new Iso52016ConstructionDiagnostics(
                "Iso52016Construction.EffectiveCapacityZero",
                "Effective internal heat capacity resolved to zero."));
        }

        return effectiveTotal;
    }

    private IReadOnlyList<Iso52016ConstructionNode> BuildFiveNodeDescriptor(
        Iso52016ConstructionMassClass massClass,
        double totalResistance,
        double effectiveInternalHeatCapacity)
    {
        var shares = _referenceDataProvider.GetFiveNodeCapacityShareFractions(massClass);
        if (shares.Count != 5)
            throw new InvalidOperationException("Five-node distribution must provide exactly 5 capacity share values.");

        var nodeNames = new[]
        {
            "InternalSurfaceNode",
            "InternalMassNode",
            "CoreMassNode",
            "ExternalMassNode",
            "ExternalSurfaceNode"
        };

        const double resistanceSharePerNode = 0.2;
        var nodes = new Iso52016ConstructionNode[5];
        for (var index = 0; index < 5; index++)
        {
            var capacityShare = Math.Clamp(shares[index], 0.0, 1.0);
            var nodeCapacity = effectiveInternalHeatCapacity * capacityShare;
            nodes[index] = new Iso52016ConstructionNode(
                NodeId: $"N{index + 1}",
                Name: nodeNames[index],
                CapacityShareFraction: Round6(capacityShare),
                CapacityJPerM2K: Round6(nodeCapacity),
                ResistanceShareFraction: Round6(resistanceSharePerNode));
        }

        _ = totalResistance;
        return nodes;
    }

    private static double ResolveSurfaceResistance(
        double? input,
        double fallback,
        string code,
        string message,
        ICollection<Iso52016ConstructionDiagnostics> diagnostics)
    {
        if (input.HasValue && input.Value > 0.0)
            return input.Value;

        diagnostics.Add(new Iso52016ConstructionDiagnostics(code, message));
        return fallback;
    }

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
