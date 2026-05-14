using System.Text;
using System.Globalization;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed partial class EnergyPlusModelExporter
{
    private static class EnergyPlusWindowSectionBuilder
    {
        public static void Append(
            StringBuilder builder,
            Room room,
            RoomGeometry geometry,
            string zoneName,
            ConstructionRegistry constructions)
        {
            var windowsByDirection = room.Windows
                .GroupBy(window => NormalizeDirection(window.Orientation))
                .ToDictionary(group => group.Key, group => group.ToArray());

            foreach (var (direction, windows) in windowsByDirection)
            {
                for (var i = 0; i < windows.Length; i++)
                {
                    var window = windows[i];
                    var constructionName = constructions.GetWindowConstruction(window);
                    var vertices = geometry.GetWindowVertices(direction, window.Area.SquareMeters, i, windows.Length);

                    AppendObject(
                        builder,
                        "FenestrationSurface:Detailed",
                        $"{zoneName}_Window_{direction}_{i + 1}",
                        "Window",
                        constructionName,
                        GetWallSurfaceName(zoneName, direction),
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        "1",
                        vertices.Count.ToString(CultureInfo.InvariantCulture),
                        FormatPoint(vertices[0]),
                        FormatPoint(vertices[1]),
                        FormatPoint(vertices[2]),
                        FormatPoint(vertices[3]));
                }
            }
        }
    }
}



