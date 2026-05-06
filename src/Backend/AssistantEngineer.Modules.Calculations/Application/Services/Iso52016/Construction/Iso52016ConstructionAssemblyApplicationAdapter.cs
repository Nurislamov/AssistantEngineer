using AssistantEngineer.Modules.Buildings.Domain.Construction;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;

public class Iso52016ConstructionAssemblyApplicationAdapter
{
    private readonly Iso52016ConstructionOptions _options;

    public Iso52016ConstructionAssemblyApplicationAdapter(
        IOptions<Iso52016ConstructionOptions>? options = null)
    {
        _options = options?.Value ?? new Iso52016ConstructionOptions();
    }

    public virtual IReadOnlyList<Iso52016ConstructionAssembly> BuildWallAssemblies(
        Room room,
        Iso52016RoomSimulationDefaults defaults)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(defaults);

        var assemblies = new List<Iso52016ConstructionAssembly>();
        foreach (var wall in room.Walls.Where(IsHeatTransferWall))
        {
            var areaM2 = Math.Max(0.0, wall.Area.SquareMeters);
            if (areaM2 <= 0.0)
                continue;

            if (wall.ConstructionAssembly is { Layers.Count: > 0 } explicitAssembly)
            {
                assemblies.Add(MapDomainAssembly(wall, explicitAssembly, areaM2));
                continue;
            }

            assemblies.Add(BuildFallbackAssemblyFromUValue(
                assemblyId: BuildEquivalentAssemblyId(wall),
                name: $"CompatibilityEquivalent-Wall-{wall.Id}",
                boundaryKind: MapBoundaryKind(wall.BoundaryType),
                areaM2: areaM2,
                resolvedUValueWPerM2K: RoomTransmissionInputFactory.ResolveWallUValue(wall),
                effectiveInternalHeatCapacityJPerM2K: null,
                internalSurfaceResistanceM2KPerW: _options.DefaultInternalSurfaceResistanceM2KPerW,
                externalSurfaceResistanceM2KPerW: _options.DefaultExternalSurfaceResistanceM2KPerW));
        }

        return assemblies;
    }

    public virtual Iso52016ConstructionAssembly BuildFallbackAssemblyFromUValue(
        string assemblyId,
        string name,
        Iso52016ConstructionBoundaryKind boundaryKind,
        double areaM2,
        double resolvedUValueWPerM2K,
        double? effectiveInternalHeatCapacityJPerM2K = null,
        double? internalSurfaceResistanceM2KPerW = null,
        double? externalSurfaceResistanceM2KPerW = null)
    {
        if (string.IsNullOrWhiteSpace(assemblyId))
            throw new ArgumentException("Assembly id is required.", nameof(assemblyId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Assembly name is required.", nameof(name));

        if (!double.IsFinite(resolvedUValueWPerM2K) || resolvedUValueWPerM2K <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(resolvedUValueWPerM2K), "Resolved U-value must be greater than zero.");

        var internalResistance = (internalSurfaceResistanceM2KPerW ?? _options.DefaultInternalSurfaceResistanceM2KPerW) > 0.0
            ? internalSurfaceResistanceM2KPerW ?? _options.DefaultInternalSurfaceResistanceM2KPerW
            : _options.DefaultInternalSurfaceResistanceM2KPerW;
        var externalResistance = (externalSurfaceResistanceM2KPerW ?? _options.DefaultExternalSurfaceResistanceM2KPerW) >= 0.0
            ? externalSurfaceResistanceM2KPerW ?? _options.DefaultExternalSurfaceResistanceM2KPerW
            : _options.DefaultExternalSurfaceResistanceM2KPerW;

        var targetTotalResistanceM2KPerW = 1.0 / resolvedUValueWPerM2K;
        var layerResistanceM2KPerW = Math.Max(0.001, targetTotalResistanceM2KPerW - internalResistance - externalResistance);

        var layers = new List<Iso52016ConstructionMaterialLayer>
        {
            new(
                LayerId: $"{assemblyId}-compat-r",
                Name: "CompatibilityEquivalentResistanceLayer",
                ThicknessM: 0.001,
                ConductivityWPerMK: 1.0,
                DensityKgPerM3: 1.0,
                SpecificHeatJPerKgK: 1.0,
                ThermalResistanceM2KPerW: layerResistanceM2KPerW,
                IsMassless: true)
        };

        if (effectiveInternalHeatCapacityJPerM2K is > 0.0 and var effectiveCapacity)
        {
            const double fallbackLayerDensityKgPerM3 = 1200.0;
            const double fallbackLayerSpecificHeatJPerKgK = 1000.0;
            const double fallbackLayerConductivityWPerMK = 1_000_000.0;
            var thicknessM = Math.Max(
                0.001,
                effectiveCapacity / (fallbackLayerDensityKgPerM3 * fallbackLayerSpecificHeatJPerKgK));

            layers.Add(new Iso52016ConstructionMaterialLayer(
                LayerId: $"{assemblyId}-compat-c",
                Name: "CompatibilityEquivalentCapacityLayer",
                ThicknessM: thicknessM,
                ConductivityWPerMK: fallbackLayerConductivityWPerMK,
                DensityKgPerM3: fallbackLayerDensityKgPerM3,
                SpecificHeatJPerKgK: fallbackLayerSpecificHeatJPerKgK));
        }

        return new Iso52016ConstructionAssembly(
            AssemblyId: assemblyId,
            Name: name,
            BoundaryKind: boundaryKind,
            Layers: layers,
            InternalSurfaceResistanceM2KPerW: internalResistance,
            ExternalSurfaceResistanceM2KPerW: externalResistance,
            AreaM2: Math.Max(0.0, areaM2));
    }

    private static Iso52016ConstructionAssembly MapDomainAssembly(
        Wall wall,
        ConstructionAssembly assembly,
        double areaM2)
    {
        var layers = assembly.Layers
            .Select((layer, index) => new Iso52016ConstructionMaterialLayer(
                LayerId: $"wall-{wall.Id}-layer-{index + 1}",
                Name: layer.Material.Name,
                ThicknessM: layer.ThicknessM,
                ConductivityWPerMK: layer.Material.ThermalConductivityWPerMK,
                DensityKgPerM3: layer.Material.DensityKgPerM3,
                SpecificHeatJPerKgK: layer.Material.SpecificHeatJPerKgK))
            .ToArray();

        return new Iso52016ConstructionAssembly(
            AssemblyId: BuildExplicitAssemblyId(wall, assembly),
            Name: assembly.Name,
            BoundaryKind: MapBoundaryKind(wall.BoundaryType),
            Layers: layers,
            AreaM2: areaM2);
    }

    private static bool IsHeatTransferWall(Wall wall) =>
        wall.BoundaryType is WallBoundaryType.External or WallBoundaryType.Ground or WallBoundaryType.AdjacentUnconditioned;

    private static string BuildExplicitAssemblyId(Wall wall, ConstructionAssembly assembly) =>
        $"wall-{wall.Id}-explicit-{assembly.Id}";

    private static string BuildEquivalentAssemblyId(Wall wall) =>
        $"wall-{wall.Id}-compat-equivalent-generated";

    private static Iso52016ConstructionBoundaryKind MapBoundaryKind(WallBoundaryType boundaryType) =>
        boundaryType switch
        {
            WallBoundaryType.Ground => Iso52016ConstructionBoundaryKind.GroundFloor,
            WallBoundaryType.AdjacentUnconditioned => Iso52016ConstructionBoundaryKind.AdjacentUnconditioned,
            WallBoundaryType.External => Iso52016ConstructionBoundaryKind.ExternalWall,
            _ => Iso52016ConstructionBoundaryKind.ExternalWall
        };
}
