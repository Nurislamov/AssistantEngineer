using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

public sealed class ThermalBoundaryConditionResolver : IThermalBoundaryConditionResolver
{
    public ThermalBoundaryResolutionResult Resolve(
        ThermalTopologySurface surface,
        BuildingThermalTopology topology)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(topology);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        ValidateSourceReferences(surface, topology, diagnostics);

        var isResolved = true;
        var isHeatTransferBoundary = false;
        var requiresOutdoorTemperature = false;
        var requiresGroundTemperature = false;
        var requiresAdjacentZoneTemperature = false;
        var requiresAdjacentUnconditionedTemperature = false;
        var isAdiabatic = false;

        switch (surface.BoundaryKind)
        {
            case ThermalBoundaryKind.Outdoor:
                isHeatTransferBoundary = true;
                requiresOutdoorTemperature = true;
                break;

            case ThermalBoundaryKind.Ground:
                isHeatTransferBoundary = true;
                requiresGroundTemperature = true;
                break;

            case ThermalBoundaryKind.AdjacentConditionedZone:
                isHeatTransferBoundary = true;
                requiresAdjacentZoneTemperature = true;
                if (string.IsNullOrWhiteSpace(surface.AdjacentZoneId) &&
                    string.IsNullOrWhiteSpace(surface.AdjacentRoomId))
                {
                    diagnostics.Add(CreateWarning(
                        "Topology.Resolver.AdjacentConditionedMissingReference",
                        $"Surface '{surface.SurfaceId}' is adjacent-conditioned but has no adjacent zone/room reference."));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.AdjacentUnconditionedZone:
                isHeatTransferBoundary = true;
                requiresAdjacentUnconditionedTemperature = true;
                if (string.IsNullOrWhiteSpace(surface.BoundarySource) &&
                    string.IsNullOrWhiteSpace(surface.AdjacentZoneId) &&
                    string.IsNullOrWhiteSpace(surface.AdjacentRoomId))
                {
                    diagnostics.Add(CreateWarning(
                        "Topology.Resolver.AdjacentUnconditionedMetadataMissing",
                        $"Surface '{surface.SurfaceId}' is adjacent-unconditioned but has no adjacent reference metadata."));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.Adiabatic:
                isHeatTransferBoundary = false;
                isAdiabatic = true;
                break;

            case ThermalBoundaryKind.InternalPartition:
                isHeatTransferBoundary = true;
                if (!string.IsNullOrWhiteSpace(surface.AdjacentZoneId) ||
                    !string.IsNullOrWhiteSpace(surface.AdjacentRoomId))
                {
                    requiresAdjacentZoneTemperature = true;
                }
                else
                {
                    diagnostics.Add(CreateWarning(
                        "Topology.Resolver.InternalPartitionMissingReference",
                        $"Surface '{surface.SurfaceId}' is an internal partition but has no adjacent room/zone reference."));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.Other:
                diagnostics.Add(CreateWarning(
                    "Topology.Resolver.OtherBoundaryKindRequiresExplicitResolution",
                    $"Surface '{surface.SurfaceId}' uses boundary kind Other and requires explicit project-specific resolution."));
                isResolved = false;
                break;

            default:
                diagnostics.Add(CreateWarning(
                    "Topology.Resolver.UnsupportedBoundaryKind",
                    $"Surface '{surface.SurfaceId}' has unsupported boundary kind '{surface.BoundaryKind}'."));
                isResolved = false;
                break;
        }

        return new ThermalBoundaryResolutionResult(
            IsResolved: isResolved && diagnostics.Count == 0,
            BoundaryKind: surface.BoundaryKind,
            SourceZoneId: surface.ZoneId,
            SourceRoomId: surface.RoomId,
            AdjacentZoneId: surface.AdjacentZoneId,
            AdjacentRoomId: surface.AdjacentRoomId,
            IsHeatTransferBoundary: isHeatTransferBoundary,
            RequiresOutdoorTemperature: requiresOutdoorTemperature,
            RequiresGroundTemperature: requiresGroundTemperature,
            RequiresAdjacentZoneTemperature: requiresAdjacentZoneTemperature,
            RequiresAdjacentUnconditionedTemperature: requiresAdjacentUnconditionedTemperature,
            IsAdiabatic: isAdiabatic,
            Diagnostics: diagnostics);
    }

    private static void ValidateSourceReferences(
        ThermalTopologySurface surface,
        BuildingThermalTopology topology,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(surface.ZoneId) &&
            !topology.Zones.Any(zone => string.Equals(zone.ZoneId, surface.ZoneId, StringComparison.Ordinal)))
        {
            diagnostics.Add(CreateWarning(
                "Topology.Resolver.SourceZoneMissing",
                $"Surface '{surface.SurfaceId}' references source zone '{surface.ZoneId}' that does not exist in topology."));
        }

        if (!string.IsNullOrWhiteSpace(surface.RoomId) &&
            !topology.Rooms.Any(room => string.Equals(room.RoomId, surface.RoomId, StringComparison.Ordinal)))
        {
            diagnostics.Add(CreateWarning(
                "Topology.Resolver.SourceRoomMissing",
                $"Surface '{surface.SurfaceId}' references source room '{surface.RoomId}' that does not exist in topology."));
        }

        if (!string.IsNullOrWhiteSpace(surface.AdjacentZoneId) &&
            !topology.Zones.Any(zone => string.Equals(zone.ZoneId, surface.AdjacentZoneId, StringComparison.Ordinal)))
        {
            diagnostics.Add(CreateWarning(
                "Topology.Resolver.AdjacentZoneMissing",
                $"Surface '{surface.SurfaceId}' references adjacent zone '{surface.AdjacentZoneId}' that does not exist in topology."));
        }

        if (!string.IsNullOrWhiteSpace(surface.AdjacentRoomId) &&
            !topology.Rooms.Any(room => string.Equals(room.RoomId, surface.AdjacentRoomId, StringComparison.Ordinal)))
        {
            diagnostics.Add(CreateWarning(
                "Topology.Resolver.AdjacentRoomMissing",
                $"Surface '{surface.SurfaceId}' references adjacent room '{surface.AdjacentRoomId}' that does not exist in topology."));
        }
    }

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        ThermalTopologyDiagnosticsFactory.Create(
            severity: CalculationDiagnosticSeverity.Warning,
            code: code,
            message: message,
            context: "ThermalBoundaryConditionResolver",
            stage: StandardCalculationStage.BoundaryCondition);
}
