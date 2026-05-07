using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

public sealed class ThermalZoneBoundaryCalculator : IThermalZoneBoundaryCalculator
{
    private readonly IThermalBoundaryConditionResolver _boundaryConditionResolver;
    private readonly IThermalTopologyValidator _topologyValidator;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public ThermalZoneBoundaryCalculator(
        IThermalBoundaryConditionResolver boundaryConditionResolver,
        IThermalTopologyValidator topologyValidator,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _boundaryConditionResolver = boundaryConditionResolver ?? throw new ArgumentNullException(nameof(boundaryConditionResolver));
        _topologyValidator = topologyValidator ?? throw new ArgumentNullException(nameof(topologyValidator));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public BuildingThermalBoundaryCalculationResult Calculate(ThermalZoneBoundaryCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Topology);

        var topology = input.Topology;
        var diagnostics = new List<StandardCalculationDiagnostic>();

        var validation = _topologyValidator.Validate(topology);
        diagnostics.AddRange(validation.Diagnostics);
        if (!validation.IsValid)
        {
            diagnostics.Add(CreateDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "AE-ZONES-TOPOLOGY-INVALID",
                "Thermal topology validation reported diagnostics; boundary calculation continues with best-effort deterministic aggregation.",
                StandardCalculationStage.Diagnostics));
        }

        var zoneTemperatures = input.ZoneAirTemperaturesCelsius is null
            ? new Dictionary<string, double>(StringComparer.Ordinal)
            : new Dictionary<string, double>(input.ZoneAirTemperaturesCelsius, StringComparer.Ordinal);
        var adjacentUnconditionedTemperatures = input.AdjacentUnconditionedTemperaturesCelsius is null
            ? new Dictionary<string, double>(StringComparer.Ordinal)
            : new Dictionary<string, double>(input.AdjacentUnconditionedTemperaturesCelsius, StringComparer.Ordinal);

        var roomsById = topology.Rooms
            .Where(room => !string.IsNullOrWhiteSpace(room.RoomId))
            .GroupBy(room => room.RoomId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var surfaceResults = topology.Surfaces
            .Select(surface => CalculateSurfaceResult(
                surface,
                topology,
                roomsById,
                zoneTemperatures,
                adjacentUnconditionedTemperatures,
                input.OutdoorTemperatureCelsius,
                input.GroundTemperatureCelsius))
            .ToArray();

        diagnostics.AddRange(surfaceResults.SelectMany(result => result.Diagnostics));

        var surfaceResultsByRoomId = surfaceResults
            .Where(result => !string.IsNullOrWhiteSpace(result.RoomId))
            .GroupBy(result => result.RoomId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<ThermalSurfaceBoundaryCalculationResult>)group.ToArray(), StringComparer.Ordinal);

        var roomResults = topology.Rooms
            .Select(room =>
            {
                var roomSurfaces = surfaceResultsByRoomId.TryGetValue(room.RoomId, out var mappedSurfaces)
                    ? mappedSurfaces
                    : [];
                var totals = AggregateSurfaceTotals(roomSurfaces);

                return new ThermalRoomBoundaryCalculationResult(
                    RoomId: room.RoomId,
                    ZoneId: room.ZoneId,
                    TotalHeatTransferCoefficientWPerKelvin: totals.TotalHeatTransferCoefficientWPerKelvin,
                    OutdoorHeatTransferCoefficientWPerKelvin: totals.OutdoorHeatTransferCoefficientWPerKelvin,
                    GroundHeatTransferCoefficientWPerKelvin: totals.GroundHeatTransferCoefficientWPerKelvin,
                    AdjacentConditionedHeatTransferCoefficientWPerKelvin: totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin,
                    AdjacentUnconditionedHeatTransferCoefficientWPerKelvin: totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin,
                    InternalPartitionHeatTransferCoefficientWPerKelvin: totals.InternalPartitionHeatTransferCoefficientWPerKelvin,
                    AdiabaticAreaSquareMeters: totals.AdiabaticAreaSquareMeters,
                    Surfaces: roomSurfaces,
                    Diagnostics: room.Diagnostics);
            })
            .ToArray();

        var roomResultsById = roomResults
            .Where(room => !string.IsNullOrWhiteSpace(room.RoomId))
            .GroupBy(room => room.RoomId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var zoneResults = topology.Zones
            .Select(zone =>
            {
                var zoneRooms = roomResults
                    .Where(room => string.Equals(room.ZoneId, zone.ZoneId, StringComparison.Ordinal))
                    .ToArray();

                var zoneUnassignedSurfaces = surfaceResults
                    .Where(surface =>
                        string.IsNullOrWhiteSpace(surface.RoomId) &&
                        string.Equals(surface.ZoneId, zone.ZoneId, StringComparison.Ordinal))
                    .ToArray();

                var zoneTotals = AggregateZoneTotals(zoneRooms, zoneUnassignedSurfaces);

                return new ThermalZoneBoundaryCalculationResult(
                    ZoneId: zone.ZoneId,
                    Name: zone.Name,
                    TotalHeatTransferCoefficientWPerKelvin: zoneTotals.TotalHeatTransferCoefficientWPerKelvin,
                    OutdoorHeatTransferCoefficientWPerKelvin: zoneTotals.OutdoorHeatTransferCoefficientWPerKelvin,
                    GroundHeatTransferCoefficientWPerKelvin: zoneTotals.GroundHeatTransferCoefficientWPerKelvin,
                    AdjacentConditionedHeatTransferCoefficientWPerKelvin: zoneTotals.AdjacentConditionedHeatTransferCoefficientWPerKelvin,
                    AdjacentUnconditionedHeatTransferCoefficientWPerKelvin: zoneTotals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin,
                    InternalPartitionHeatTransferCoefficientWPerKelvin: zoneTotals.InternalPartitionHeatTransferCoefficientWPerKelvin,
                    AdiabaticAreaSquareMeters: zoneTotals.AdiabaticAreaSquareMeters,
                    Rooms: zoneRooms,
                    UnassignedSurfaces: zoneUnassignedSurfaces,
                    Diagnostics: zone.Diagnostics);
            })
            .ToArray();

        var unassignedRooms = roomResults
            .Where(room => string.IsNullOrWhiteSpace(room.ZoneId))
            .ToArray();

        var unassignedSurfaces = surfaceResults
            .Where(surface => string.IsNullOrWhiteSpace(surface.RoomId) && string.IsNullOrWhiteSpace(surface.ZoneId))
            .ToArray();

        var buildingTotals = AggregateBuildingTotals(zoneResults, unassignedRooms, unassignedSurfaces);

        var baseDisclosure = _disclosureFactory.CreateThermalZonesDisclosure();
        var disclosure = MergeDisclosure(baseDisclosure, input.DisclosureOverride, diagnostics);

        return new BuildingThermalBoundaryCalculationResult(
            BuildingId: topology.BuildingId,
            TotalHeatTransferCoefficientWPerKelvin: buildingTotals.TotalHeatTransferCoefficientWPerKelvin,
            OutdoorHeatTransferCoefficientWPerKelvin: buildingTotals.OutdoorHeatTransferCoefficientWPerKelvin,
            GroundHeatTransferCoefficientWPerKelvin: buildingTotals.GroundHeatTransferCoefficientWPerKelvin,
            AdjacentConditionedHeatTransferCoefficientWPerKelvin: buildingTotals.AdjacentConditionedHeatTransferCoefficientWPerKelvin,
            AdjacentUnconditionedHeatTransferCoefficientWPerKelvin: buildingTotals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin,
            InternalPartitionHeatTransferCoefficientWPerKelvin: buildingTotals.InternalPartitionHeatTransferCoefficientWPerKelvin,
            AdiabaticAreaSquareMeters: buildingTotals.AdiabaticAreaSquareMeters,
            Zones: zoneResults,
            UnassignedRooms: unassignedRooms,
            UnassignedSurfaces: unassignedSurfaces,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    private ThermalSurfaceBoundaryCalculationResult CalculateSurfaceResult(
        ThermalTopologySurface surface,
        BuildingThermalTopology topology,
        IReadOnlyDictionary<string, ThermalTopologyRoom> roomsById,
        IReadOnlyDictionary<string, double> zoneTemperatures,
        IReadOnlyDictionary<string, double> adjacentUnconditionedTemperatures,
        double? outdoorTemperatureCelsius,
        double? groundTemperatureCelsius)
    {
        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(surface.Diagnostics);

        var resolution = _boundaryConditionResolver.Resolve(surface, topology);
        diagnostics.AddRange(resolution.Diagnostics);

        var sourceZoneId = ResolveSourceZoneId(surface, roomsById);
        var sourceZoneTemperature = TryGetTemperature(sourceZoneId, zoneTemperatures);

        var isResolved = resolution.IsResolved;
        double? boundaryTemperature = null;
        double? adjacentTemperature = null;

        switch (surface.BoundaryKind)
        {
            case ThermalBoundaryKind.Outdoor:
                if (outdoorTemperatureCelsius.HasValue)
                {
                    boundaryTemperature = outdoorTemperatureCelsius.Value;
                }
                else
                {
                    diagnostics.Add(CreateDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-OUTDOOR-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' requires outdoor temperature but no value was provided.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.Ground:
                if (groundTemperatureCelsius.HasValue)
                {
                    boundaryTemperature = groundTemperatureCelsius.Value;
                }
                else
                {
                    diagnostics.Add(CreateDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-GROUND-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' requires ground temperature but no value was provided.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.AdjacentConditionedZone:
                adjacentTemperature = ResolveAdjacentConditionedTemperature(surface, roomsById, zoneTemperatures);
                boundaryTemperature = adjacentTemperature;
                if (!adjacentTemperature.HasValue)
                {
                    diagnostics.Add(CreateDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-ADJACENT-ZONE-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' adjacent conditioned temperature could not be resolved.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                if (!sourceZoneTemperature.HasValue)
                {
                    diagnostics.Add(CreateDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-SOURCE-ZONE-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' has no source zone temperature for adjacent conditioned interpretation.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.AdjacentUnconditionedZone:
                adjacentTemperature = ResolveAdjacentUnconditionedTemperature(surface, adjacentUnconditionedTemperatures);
                boundaryTemperature = adjacentTemperature;
                if (!adjacentTemperature.HasValue)
                {
                    diagnostics.Add(CreateDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-ADJACENT-UNCONDITIONED-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' adjacent unconditioned temperature could not be resolved.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.InternalPartition:
                adjacentTemperature = ResolveAdjacentConditionedTemperature(surface, roomsById, zoneTemperatures);
                boundaryTemperature = adjacentTemperature;
                if (!adjacentTemperature.HasValue)
                {
                    diagnostics.Add(CreateDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-INTERNAL-PARTITION-UNRESOLVED",
                        $"Surface '{surface.SurfaceId}' internal partition temperature could not be resolved from adjacent zone/room.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                if (!sourceZoneTemperature.HasValue)
                {
                    diagnostics.Add(CreateDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-SOURCE-ZONE-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' has no source zone temperature for internal partition interpretation.",
                        StandardCalculationStage.BoundaryCondition));
                    isResolved = false;
                }

                break;

            case ThermalBoundaryKind.Adiabatic:
                boundaryTemperature = sourceZoneTemperature;
                if (!sourceZoneTemperature.HasValue)
                {
                    diagnostics.Add(CreateDiagnostic(
                        CalculationDiagnosticSeverity.Warning,
                        "AE-ZONES-SOURCE-ZONE-TEMPERATURE-MISSING",
                        $"Surface '{surface.SurfaceId}' adiabatic interpretation expects source zone temperature.",
                        StandardCalculationStage.BoundaryCondition));
                }

                break;

            case ThermalBoundaryKind.Other:
                diagnostics.Add(CreateDiagnostic(
                    CalculationDiagnosticSeverity.Warning,
                    "AE-ZONES-BOUNDARY-OTHER-UNSUPPORTED",
                    $"Surface '{surface.SurfaceId}' boundary kind Other is unsupported for deterministic boundary calculation.",
                    StandardCalculationStage.BoundaryCondition));
                isResolved = false;
                break;
        }

        if (!(surface.AreaSquareMeters > 0.0))
        {
            diagnostics.Add(CreateDiagnostic(
                CalculationDiagnosticSeverity.Error,
                "AE-ZONES-SURFACE-AREA-NONPOSITIVE",
                $"Surface '{surface.SurfaceId}' area must be greater than zero.",
                StandardCalculationStage.HeatTransfer));
            isResolved = false;
        }

        double? heatTransferCoefficient = null;
        if (surface.BoundaryKind == ThermalBoundaryKind.Adiabatic)
        {
            heatTransferCoefficient = 0.0;
        }
        else if (resolution.IsHeatTransferBoundary)
        {
            if (!surface.UValueWPerSquareMeterKelvin.HasValue)
            {
                diagnostics.Add(CreateDiagnostic(
                    CalculationDiagnosticSeverity.Warning,
                    "AE-ZONES-SURFACE-UVALUE-MISSING",
                    $"Surface '{surface.SurfaceId}' requires a U-value for heat transfer coefficient calculation.",
                    StandardCalculationStage.HeatTransfer));
                isResolved = false;
            }
            else if (!(surface.UValueWPerSquareMeterKelvin.Value > 0.0))
            {
                diagnostics.Add(CreateDiagnostic(
                    CalculationDiagnosticSeverity.Error,
                    "AE-ZONES-SURFACE-UVALUE-NONPOSITIVE",
                    $"Surface '{surface.SurfaceId}' U-value must be greater than zero.",
                    StandardCalculationStage.HeatTransfer));
                isResolved = false;
            }
            else if (surface.AreaSquareMeters > 0.0)
            {
                heatTransferCoefficient = surface.AreaSquareMeters * surface.UValueWPerSquareMeterKelvin.Value;
            }
        }

        var effectiveZoneId = sourceZoneId ?? surface.ZoneId;

        return new ThermalSurfaceBoundaryCalculationResult(
            SurfaceId: surface.SurfaceId,
            RoomId: surface.RoomId,
            ZoneId: effectiveZoneId,
            BoundaryKind: surface.BoundaryKind,
            AreaSquareMeters: surface.AreaSquareMeters,
            UValueWPerSquareMeterKelvin: surface.UValueWPerSquareMeterKelvin,
            HeatTransferCoefficientWPerKelvin: heatTransferCoefficient,
            BoundaryTemperatureCelsius: boundaryTemperature,
            SourceZoneTemperatureCelsius: sourceZoneTemperature,
            AdjacentTemperatureCelsius: adjacentTemperature,
            IsHeatTransferBoundary: resolution.IsHeatTransferBoundary,
            IsAdiabatic: surface.BoundaryKind == ThermalBoundaryKind.Adiabatic || resolution.IsAdiabatic,
            IsResolved: isResolved,
            Diagnostics: diagnostics);
    }

    private static string? ResolveSourceZoneId(
        ThermalTopologySurface surface,
        IReadOnlyDictionary<string, ThermalTopologyRoom> roomsById)
    {
        if (!string.IsNullOrWhiteSpace(surface.ZoneId))
            return surface.ZoneId;

        if (!string.IsNullOrWhiteSpace(surface.RoomId) &&
            roomsById.TryGetValue(surface.RoomId, out var room) &&
            !string.IsNullOrWhiteSpace(room.ZoneId))
        {
            return room.ZoneId;
        }

        return null;
    }

    private static double? ResolveAdjacentConditionedTemperature(
        ThermalTopologySurface surface,
        IReadOnlyDictionary<string, ThermalTopologyRoom> roomsById,
        IReadOnlyDictionary<string, double> zoneTemperatures)
    {
        if (!string.IsNullOrWhiteSpace(surface.AdjacentZoneId))
        {
            var zoneTemperature = TryGetTemperature(surface.AdjacentZoneId, zoneTemperatures);
            if (zoneTemperature.HasValue)
                return zoneTemperature;
        }

        if (!string.IsNullOrWhiteSpace(surface.AdjacentRoomId) &&
            roomsById.TryGetValue(surface.AdjacentRoomId, out var adjacentRoom) &&
            !string.IsNullOrWhiteSpace(adjacentRoom.ZoneId))
        {
            return TryGetTemperature(adjacentRoom.ZoneId, zoneTemperatures);
        }

        return null;
    }

    private static double? ResolveAdjacentUnconditionedTemperature(
        ThermalTopologySurface surface,
        IReadOnlyDictionary<string, double> adjacentUnconditionedTemperatures)
    {
        foreach (var candidateKey in new[] { surface.AdjacentZoneId, surface.AdjacentRoomId, surface.BoundarySource })
        {
            var value = TryGetTemperature(candidateKey, adjacentUnconditionedTemperatures);
            if (value.HasValue)
                return value;
        }

        return null;
    }

    private static double? TryGetTemperature(
        string? key,
        IReadOnlyDictionary<string, double> temperatures)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (!temperatures.TryGetValue(key, out var value))
            return null;

        return double.IsFinite(value) ? value : null;
    }

    private static BoundaryTotals AggregateSurfaceTotals(
        IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> surfaces)
    {
        var totals = new BoundaryTotals();

        foreach (var surface in surfaces)
        {
            var coefficient = surface.HeatTransferCoefficientWPerKelvin ?? 0.0;
            if (!double.IsFinite(coefficient) || coefficient <= 0.0)
            {
                continue;
            }

            totals.TotalHeatTransferCoefficientWPerKelvin += coefficient;
            switch (surface.BoundaryKind)
            {
                case ThermalBoundaryKind.Outdoor:
                    totals.OutdoorHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
                case ThermalBoundaryKind.Ground:
                    totals.GroundHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
                case ThermalBoundaryKind.AdjacentConditionedZone:
                    totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
                case ThermalBoundaryKind.AdjacentUnconditionedZone:
                    totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
                case ThermalBoundaryKind.InternalPartition:
                    totals.InternalPartitionHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
            }
        }

        totals.AdiabaticAreaSquareMeters += surfaces
            .Where(surface => surface.IsAdiabatic)
            .Sum(surface => Math.Max(0.0, surface.AreaSquareMeters));

        return totals;
    }

    private static BoundaryTotals AggregateZoneTotals(
        IReadOnlyList<ThermalRoomBoundaryCalculationResult> rooms,
        IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> unassignedSurfaces)
    {
        var totals = new BoundaryTotals();

        foreach (var room in rooms)
        {
            totals.TotalHeatTransferCoefficientWPerKelvin += room.TotalHeatTransferCoefficientWPerKelvin;
            totals.OutdoorHeatTransferCoefficientWPerKelvin += room.OutdoorHeatTransferCoefficientWPerKelvin;
            totals.GroundHeatTransferCoefficientWPerKelvin += room.GroundHeatTransferCoefficientWPerKelvin;
            totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += room.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
            totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += room.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
            totals.InternalPartitionHeatTransferCoefficientWPerKelvin += room.InternalPartitionHeatTransferCoefficientWPerKelvin;
            totals.AdiabaticAreaSquareMeters += room.AdiabaticAreaSquareMeters;
        }

        var surfaceTotals = AggregateSurfaceTotals(unassignedSurfaces);
        totals.TotalHeatTransferCoefficientWPerKelvin += surfaceTotals.TotalHeatTransferCoefficientWPerKelvin;
        totals.OutdoorHeatTransferCoefficientWPerKelvin += surfaceTotals.OutdoorHeatTransferCoefficientWPerKelvin;
        totals.GroundHeatTransferCoefficientWPerKelvin += surfaceTotals.GroundHeatTransferCoefficientWPerKelvin;
        totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += surfaceTotals.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
        totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += surfaceTotals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
        totals.InternalPartitionHeatTransferCoefficientWPerKelvin += surfaceTotals.InternalPartitionHeatTransferCoefficientWPerKelvin;
        totals.AdiabaticAreaSquareMeters += surfaceTotals.AdiabaticAreaSquareMeters;

        return totals;
    }

    private static BoundaryTotals AggregateBuildingTotals(
        IReadOnlyList<ThermalZoneBoundaryCalculationResult> zones,
        IReadOnlyList<ThermalRoomBoundaryCalculationResult> unassignedRooms,
        IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> unassignedSurfaces)
    {
        var totals = new BoundaryTotals();

        foreach (var zone in zones)
        {
            totals.TotalHeatTransferCoefficientWPerKelvin += zone.TotalHeatTransferCoefficientWPerKelvin;
            totals.OutdoorHeatTransferCoefficientWPerKelvin += zone.OutdoorHeatTransferCoefficientWPerKelvin;
            totals.GroundHeatTransferCoefficientWPerKelvin += zone.GroundHeatTransferCoefficientWPerKelvin;
            totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += zone.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
            totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += zone.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
            totals.InternalPartitionHeatTransferCoefficientWPerKelvin += zone.InternalPartitionHeatTransferCoefficientWPerKelvin;
            totals.AdiabaticAreaSquareMeters += zone.AdiabaticAreaSquareMeters;
        }

        foreach (var room in unassignedRooms)
        {
            totals.TotalHeatTransferCoefficientWPerKelvin += room.TotalHeatTransferCoefficientWPerKelvin;
            totals.OutdoorHeatTransferCoefficientWPerKelvin += room.OutdoorHeatTransferCoefficientWPerKelvin;
            totals.GroundHeatTransferCoefficientWPerKelvin += room.GroundHeatTransferCoefficientWPerKelvin;
            totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += room.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
            totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += room.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
            totals.InternalPartitionHeatTransferCoefficientWPerKelvin += room.InternalPartitionHeatTransferCoefficientWPerKelvin;
            totals.AdiabaticAreaSquareMeters += room.AdiabaticAreaSquareMeters;
        }

        var unassignedSurfaceTotals = AggregateSurfaceTotals(unassignedSurfaces);
        totals.TotalHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.TotalHeatTransferCoefficientWPerKelvin;
        totals.OutdoorHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.OutdoorHeatTransferCoefficientWPerKelvin;
        totals.GroundHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.GroundHeatTransferCoefficientWPerKelvin;
        totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
        totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
        totals.InternalPartitionHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.InternalPartitionHeatTransferCoefficientWPerKelvin;
        totals.AdiabaticAreaSquareMeters += unassignedSurfaceTotals.AdiabaticAreaSquareMeters;

        return totals;
    }

    private static StandardCalculationDisclosure MergeDisclosure(
        StandardCalculationDisclosure baseDisclosure,
        StandardCalculationDisclosure? disclosureOverride,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (disclosureOverride is null)
            return baseDisclosure;

        var baseBoundary = baseDisclosure.ClaimBoundary;
        var overrideBoundary = disclosureOverride.ClaimBoundary ?? baseBoundary;

        var forbiddenClaims = overrideBoundary.ForbiddenClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (var requiredClaim in baseBoundary.ForbiddenClaims)
        {
            if (!forbiddenClaims.Contains(requiredClaim, StringComparer.Ordinal))
                forbiddenClaims.Add(requiredClaim);
        }

        var removedAllowedClaims = new List<string>();
        var allowedClaims = (overrideBoundary.AllowedClaims ?? [])
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Where(claim =>
            {
                var isForbidden = forbiddenClaims.Contains(claim, StringComparer.Ordinal);
                if (isForbidden)
                    removedAllowedClaims.Add(claim);

                return !isForbidden;
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (removedAllowedClaims.Count > 0)
        {
            diagnostics.Add(CreateDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "AE-ZONES-DISCLOSURE-OVERRIDE-SANITIZED",
                $"Disclosure override removed forbidden allowed-claim entries: {string.Join(", ", removedAllowedClaims)}.",
                StandardCalculationStage.Diagnostics));
        }

        var mergedBoundary = new StandardClaimBoundary(
            AllowedClaims: allowedClaims,
            ForbiddenClaims: forbiddenClaims,
            Limitations: overrideBoundary.Limitations ?? baseBoundary.Limitations,
            Assumptions: overrideBoundary.Assumptions ?? baseBoundary.Assumptions);

        return disclosureOverride with
        {
            CalculationPath = string.IsNullOrWhiteSpace(disclosureOverride.CalculationPath)
                ? baseDisclosure.CalculationPath
                : disclosureOverride.CalculationPath,
            ClaimBoundary = mergedBoundary
        };
    }

    private static StandardCalculationDiagnostic CreateDiagnostic(
        CalculationDiagnosticSeverity severity,
        string code,
        string message,
        StandardCalculationStage stage) =>
        new(
            Severity: severity,
            Code: code,
            Message: message,
            Context: "ThermalZoneBoundaryCalculator",
            Source: "ThermalZoneBoundaryCalculator",
            Family: StandardCalculationFamily.InternalEngineering,
            Stage: stage);

    private sealed class BoundaryTotals
    {
        public double TotalHeatTransferCoefficientWPerKelvin { get; set; }

        public double OutdoorHeatTransferCoefficientWPerKelvin { get; set; }

        public double GroundHeatTransferCoefficientWPerKelvin { get; set; }

        public double AdjacentConditionedHeatTransferCoefficientWPerKelvin { get; set; }

        public double AdjacentUnconditionedHeatTransferCoefficientWPerKelvin { get; set; }

        public double InternalPartitionHeatTransferCoefficientWPerKelvin { get; set; }

        public double AdiabaticAreaSquareMeters { get; set; }
    }
}
