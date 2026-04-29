using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Models.Heating;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Transmission;

internal static class RoomTransmissionInputFactory
{
    public static TransmissionHeatTransferRequest CreateForRoom(
        Room room,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double? groundTemperatureC = null)
    {
        var elements = new List<TransmissionElementInput>(
            room.Walls.Count + room.Windows.Count);

        foreach (var wall in room.Walls)
        {
            elements.Add(new TransmissionElementInput(
                ElementId: wall.Id,
                ElementType: TransmissionElementType.Wall,
                RoomId: room.Id,
                AreaM2: wall.Area.SquareMeters,
                UValueWPerM2K: ResolveWallUValue(wall),
                IndoorTemperatureC: indoorTemperatureC,
                BoundaryType: MapBoundaryType(wall.BoundaryType),
                OutdoorTemperatureC: wall.BoundaryType == WallBoundaryType.External ? outdoorTemperatureC : null,
                AdjacentTemperatureC: IsAdjacent(wall.BoundaryType)
                    ? wall.AdjacentRoom?.IndoorTemperature.Celsius
                    : null,
                GroundTemperatureC: wall.BoundaryType == WallBoundaryType.Ground ? groundTemperatureC : null,
                DiagnosticsContext: $"Room {room.Id} wall {wall.Id}"));
        }

        foreach (var window in room.Windows)
        {
            elements.Add(new TransmissionElementInput(
                ElementId: window.Id,
                ElementType: TransmissionElementType.Window,
                RoomId: room.Id,
                AreaM2: window.Area.SquareMeters,
                UValueWPerM2K: window.UValue.Value,
                IndoorTemperatureC: indoorTemperatureC,
                BoundaryType: TransmissionBoundaryType.Outdoor,
                OutdoorTemperatureC: outdoorTemperatureC,
                DiagnosticsContext: $"Room {room.Id} window {window.Id}"));
        }

        return new TransmissionHeatTransferRequest(elements);
    }

    public static TransmissionHeatTransferRequest CreateForReadModelRoom(
        RoomHeatingReadModel room,
        double outdoorTemperatureC)
    {
        var elements = new List<TransmissionElementInput>(
            room.Walls.Count + room.Windows.Count);

        var wallIndex = 0;
        foreach (var wall in room.Walls)
        {
            wallIndex++;
            elements.Add(new TransmissionElementInput(
                ElementId: wallIndex,
                ElementType: TransmissionElementType.Wall,
                RoomId: room.RoomId,
                AreaM2: wall.AreaM2,
                UValueWPerM2K: ResolveReadModelWallUValue(wall),
                IndoorTemperatureC: room.IndoorTemperatureC,
                BoundaryType: wall.IsExternal
                    ? TransmissionBoundaryType.Outdoor
                    : TransmissionBoundaryType.InternalAdiabatic,
                OutdoorTemperatureC: wall.IsExternal ? outdoorTemperatureC : null,
                DiagnosticsContext: $"Room {room.RoomId} wall {wallIndex}"));
        }

        var windowIndex = 0;
        foreach (var window in room.Windows)
        {
            windowIndex++;
            elements.Add(new TransmissionElementInput(
                ElementId: windowIndex,
                ElementType: TransmissionElementType.Window,
                RoomId: room.RoomId,
                AreaM2: window.AreaM2,
                UValueWPerM2K: window.UValue,
                IndoorTemperatureC: room.IndoorTemperatureC,
                BoundaryType: TransmissionBoundaryType.Outdoor,
                OutdoorTemperatureC: outdoorTemperatureC,
                DiagnosticsContext: $"Room {room.RoomId} window {windowIndex}"));
        }

        return new TransmissionHeatTransferRequest(elements);
    }

    public static double ResolveWallUValue(Wall wall) =>
        wall.ConstructionAssembly is { UValueWPerM2K: > 0 } assembly
            ? assembly.UValueWPerM2K
            : wall.UValue.Value;

    private static double ResolveReadModelWallUValue(WallHeatingReadModel wall)
    {
        if (wall.ConstructionLayers.Count == 0)
            return wall.UValue;

        const double internalSurfaceResistance = 0.13;
        const double externalSurfaceResistance = 0.04;
        var layerResistance = wall.ConstructionLayers.Sum(layer =>
            layer.ThicknessM / layer.ThermalConductivityWPerMK);
        var totalResistance = internalSurfaceResistance + layerResistance + externalSurfaceResistance;
        return totalResistance > 0 ? 1.0 / totalResistance : wall.UValue;
    }

    private static bool IsAdjacent(WallBoundaryType boundaryType) =>
        boundaryType is WallBoundaryType.AdjacentConditioned or WallBoundaryType.AdjacentUnconditioned;

    private static TransmissionBoundaryType MapBoundaryType(WallBoundaryType boundaryType) =>
        boundaryType switch
        {
            WallBoundaryType.External => TransmissionBoundaryType.Outdoor,
            WallBoundaryType.Ground => TransmissionBoundaryType.Ground,
            WallBoundaryType.AdjacentUnconditioned => TransmissionBoundaryType.AdjacentUnheatedSpace,
            WallBoundaryType.AdjacentConditioned => TransmissionBoundaryType.AdjacentConditionedZone,
            WallBoundaryType.Adiabatic => TransmissionBoundaryType.InternalAdiabatic,
            _ => TransmissionBoundaryType.InternalAdiabatic
        };
}
