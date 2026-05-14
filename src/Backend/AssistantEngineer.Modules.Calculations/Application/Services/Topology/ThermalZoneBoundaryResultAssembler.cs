using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

internal static class ThermalZoneBoundaryResultAssembler
{
    public static IReadOnlyList<ThermalRoomBoundaryCalculationResult> BuildRoomResults(
        IReadOnlyList<ThermalTopologyRoom> rooms,
        IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> surfaceResults)
    {
        var surfaceResultsByRoomId = surfaceResults
            .Where(result => !string.IsNullOrWhiteSpace(result.RoomId))
            .GroupBy(result => result.RoomId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<ThermalSurfaceBoundaryCalculationResult>)group.ToArray(), StringComparer.Ordinal);

        return rooms
            .Select(room =>
            {
                var roomSurfaces = surfaceResultsByRoomId.TryGetValue(room.RoomId, out var mappedSurfaces)
                    ? mappedSurfaces
                    : [];
                var totals = ThermalZoneBoundaryAggregation.AggregateSurfaceTotals(roomSurfaces);

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
    }

    public static IReadOnlyList<ThermalZoneBoundaryCalculationResult> BuildZoneResults(
        IReadOnlyList<ThermalTopologyZone> zones,
        IReadOnlyList<ThermalRoomBoundaryCalculationResult> roomResults,
        IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> surfaceResults)
    {
        return zones
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

                var zoneTotals = ThermalZoneBoundaryAggregation.AggregateZoneTotals(zoneRooms, zoneUnassignedSurfaces);

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
    }
}
