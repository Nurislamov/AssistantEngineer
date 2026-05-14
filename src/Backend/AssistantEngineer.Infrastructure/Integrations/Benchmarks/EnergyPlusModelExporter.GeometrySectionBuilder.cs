using System.Text;
using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed partial class EnergyPlusModelExporter
{
    private static class EnergyPlusGeometrySectionBuilder
    {
        public static void Append(
            StringBuilder builder,
            RoomPlacement placement,
            IReadOnlyList<RoomPlacement> allPlacements,
            string zoneName,
            ConstructionRegistry constructions)
        {
            AppendZone(builder, zoneName);
            AppendRoomSurfaces(builder, placement, allPlacements, zoneName, constructions);
        }

        public static void AppendZone(StringBuilder builder, string zoneName)
        {
            AppendObject(
                builder,
                "Zone",
                zoneName,
                "0",
                "0",
                "0",
                "0",
                "1",
                "1",
                "autocalculate",
                "autocalculate");
        }

        public static void AppendRoomSurfaces(
            StringBuilder builder,
            RoomPlacement placement,
            IReadOnlyList<RoomPlacement> allPlacements,
            string zoneName,
            ConstructionRegistry constructions)
        {
            var geometry = placement.Geometry;
            AppendSurface(
                builder,
                $"{zoneName}_Floor",
                "Floor",
                "AE_Generic_Floor",
                zoneName,
                "Ground",
                string.Empty,
                "NoSun",
                "NoWind",
                geometry.FloorVertices);

            AppendSurface(
                builder,
                $"{zoneName}_Roof",
                "Roof",
                "AE_Generic_Roof",
                zoneName,
                "Outdoors",
                string.Empty,
                "SunExposed",
                "WindExposed",
                geometry.RoofVertices);

            AppendWallSurface(builder, placement, allPlacements, zoneName, CardinalDirection.North, geometry.NorthWallVertices, constructions);
            AppendWallSurface(builder, placement, allPlacements, zoneName, CardinalDirection.East, geometry.EastWallVertices, constructions);
            AppendWallSurface(builder, placement, allPlacements, zoneName, CardinalDirection.South, geometry.SouthWallVertices, constructions);
            AppendWallSurface(builder, placement, allPlacements, zoneName, CardinalDirection.West, geometry.WestWallVertices, constructions);
        }

        public static void AppendWallSurface(
            StringBuilder builder,
            RoomPlacement placement,
            IReadOnlyList<RoomPlacement> allPlacements,
            string zoneName,
            CardinalDirection direction,
            IReadOnlyList<Point3d> vertices,
            ConstructionRegistry constructions)
        {
            var room = placement.Room;
            var wall = FindWall(room, direction);
            var adjacentSurface = wall?.IsExternal == true
                ? null
                : FindAdjacentSurfaceName(placement, direction, allPlacements);
            var isExternal = adjacentSurface is null && (wall?.IsExternal ?? true);
            var constructionName = constructions.GetWallConstruction(wall);

            AppendSurface(
                builder,
                GetWallSurfaceName(zoneName, direction),
                "Wall",
                constructionName,
                zoneName,
                adjacentSurface is not null ? "Surface" : isExternal ? "Outdoors" : "Adiabatic",
                adjacentSurface ?? string.Empty,
                isExternal ? "SunExposed" : "NoSun",
                isExternal ? "WindExposed" : "NoWind",
                vertices);
        }
    }
}



