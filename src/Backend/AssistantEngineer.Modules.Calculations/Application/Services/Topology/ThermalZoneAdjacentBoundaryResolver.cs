using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

internal static class ThermalZoneAdjacentBoundaryResolver
{
    public static string? ResolveSourceZoneId(
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

    public static double? ResolveAdjacentConditionedTemperature(
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

    public static double? ResolveAdjacentUnconditionedTemperature(
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

    public static double? TryGetTemperature(
        string? key,
        IReadOnlyDictionary<string, double> temperatures)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (!temperatures.TryGetValue(key, out var value))
            return null;

        return double.IsFinite(value) ? value : null;
    }
}
