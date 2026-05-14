using System.Globalization;
using System.Text;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed partial class EnergyPlusModelExporter : IEnergyPlusModelExporter
{
    private const string AlwaysOnSchedule = "AE_Always_On";
    private const string HeatingSetpointSchedule = "AE_Heating_Setpoint";
    private const string CoolingSetpointSchedule = "AE_Cooling_Setpoint";
    private const string ActivitySchedule = "AE_Activity_120W";
    private readonly IEnergyPlusArtifactStore _artifacts;

    public EnergyPlusModelExporter(IEnergyPlusArtifactStore artifacts)
    {
        _artifacts = artifacts;
    }

    public async Task<Result<EnergyPlusModelExportResult>> ExportAsync(
        Building building,
        string? runName = null,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateRequest(building);
        if (validation.IsFailure)
            return Result<EnergyPlusModelExportResult>.Failure(validation);

        var artifact = _artifacts.CreateModelArtifact(building.Id, runName);
        if (artifact.IsFailure)
            return Result<EnergyPlusModelExportResult>.Failure(artifact);

        var model = BuildModel(building);
        await File.WriteAllTextAsync(artifact.Value.FileSystemPath, model, Encoding.UTF8, cancellationToken);

        return Result<EnergyPlusModelExportResult>.Success(new EnergyPlusModelExportResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            ModelArtifactId = artifact.Value.ArtifactId
        });
    }

    private static Result ValidateRequest(Building building)
    {
        if (building is null)
            return Result.Validation("Building is required for EnergyPlus model export.");

        if (!GetRooms(building).Any())
            return Result.Validation("EnergyPlus model export requires at least one room.");

        return Result.Success();
    }

    private static string BuildModel(Building building)
    {
        var builder = new StringBuilder();
        var rooms = GetRooms(building).ToArray();
        var constructions = new ConstructionRegistry(builder);

        EnergyPlusHeaderSectionBuilder.Append(builder, building);
        EnergyPlusScheduleSectionBuilder.Append(builder, rooms);

        var placements = CreateRoomPlacements(building);
        foreach (var placement in placements)
        {
            var room = placement.Room;
            var geometry = placement.Geometry;

            var zoneName = GetZoneName(room);
            EnergyPlusGeometrySectionBuilder.Append(builder, placement, placements, zoneName, constructions);
            EnergyPlusWindowSectionBuilder.Append(builder, room, geometry, zoneName, constructions);
            EnergyPlusInternalGainsSectionBuilder.Append(builder, room, zoneName);
            EnergyPlusVentilationInfiltrationSectionBuilder.Append(builder, room, zoneName);
            EnergyPlusIdealLoadsSectionBuilder.Append(builder, zoneName);
        }

        EnergyPlusOutputSectionBuilder.Append(builder);
        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, Building building)
        => EnergyPlusHeaderSectionBuilder.Append(builder, building);

    private static void AppendSchedules(StringBuilder builder, IReadOnlyCollection<Room> rooms)
        => EnergyPlusScheduleSectionBuilder.Append(builder, rooms);

    private static void AppendZone(StringBuilder builder, string zoneName)
        => EnergyPlusGeometrySectionBuilder.AppendZone(builder, zoneName);

    private static void AppendRoomSurfaces(
        StringBuilder builder,
        RoomPlacement placement,
        IReadOnlyList<RoomPlacement> allPlacements,
        string zoneName,
        ConstructionRegistry constructions)
        => EnergyPlusGeometrySectionBuilder.AppendRoomSurfaces(
            builder,
            placement,
            allPlacements,
            zoneName,
            constructions);

    private static void AppendWallSurface(
        StringBuilder builder,
        RoomPlacement placement,
        IReadOnlyList<RoomPlacement> allPlacements,
        string zoneName,
        CardinalDirection direction,
        IReadOnlyList<Point3d> vertices,
        ConstructionRegistry constructions)
        => EnergyPlusGeometrySectionBuilder.AppendWallSurface(
            builder,
            placement,
            allPlacements,
            zoneName,
            direction,
            vertices,
            constructions);

    private static void AppendRoomWindows(
        StringBuilder builder,
        Room room,
        RoomGeometry geometry,
        string zoneName,
        ConstructionRegistry constructions)
        => EnergyPlusWindowSectionBuilder.Append(
            builder,
            room,
            geometry,
            zoneName,
            constructions);

    private static void AppendInternalLoads(StringBuilder builder, Room room, string zoneName)
        => EnergyPlusInternalGainsSectionBuilder.Append(builder, room, zoneName);

    private static void AppendVentilation(StringBuilder builder, Room room, string zoneName)
        => EnergyPlusVentilationInfiltrationSectionBuilder.Append(builder, room, zoneName);

    private static void AppendIdealLoadsSystem(StringBuilder builder, string zoneName)
        => EnergyPlusIdealLoadsSectionBuilder.Append(builder, zoneName);

    private static void AppendOutputs(StringBuilder builder)
        => EnergyPlusOutputSectionBuilder.Append(builder);

    private static void AppendSurface(
        StringBuilder builder,
        string name,
        string surfaceType,
        string constructionName,
        string zoneName,
        string outsideBoundaryCondition,
        string outsideBoundaryConditionObject,
        string sunExposure,
        string windExposure,
        IReadOnlyList<Point3d> vertices)
    {
        AppendObject(
            builder,
            "BuildingSurface:Detailed",
            name,
            surfaceType,
            constructionName,
            zoneName,
            outsideBoundaryCondition,
            outsideBoundaryConditionObject,
            sunExposure,
            windExposure,
            "0.5",
            vertices.Count.ToString(CultureInfo.InvariantCulture),
            FormatPoint(vertices[0]),
            FormatPoint(vertices[1]),
            FormatPoint(vertices[2]),
            FormatPoint(vertices[3]));
    }

    private static void AppendHourlySchedule(StringBuilder builder, string name, IReadOnlyList<double>? factors)
    {
        var values = factors is { Count: 24 }
            ? factors
            : Enumerable.Repeat(1.0, 24).ToArray();

        var fields = new List<string>();

        for (var hour = 0; hour < 24; hour++)
        {
            fields.Add($"Until: {hour + 1:00}:00");
            fields.Add(F(values[hour]));
        }

        AppendCompactSchedule(builder, name, "Fraction", fields);
    }

    private static void AppendCompactSchedule(
        StringBuilder builder,
        string name,
        string scheduleTypeLimitsName,
        IReadOnlyList<string> fields)
    {
        var objectFields = new List<string>
        {
            name,
            scheduleTypeLimitsName,
            "Through: 12/31",
            "For: AllDays"
        };
        objectFields.AddRange(fields);
        AppendObject(builder, "Schedule:Compact", objectFields.ToArray());
    }

    private static void AppendObject(StringBuilder builder, string objectType, params string[] fields)
    {
        builder.AppendLine(objectType + ",");
        for (var i = 0; i < fields.Length; i++)
        {
            builder.Append("  ");
            builder.Append(fields[i]);
            builder.AppendLine(i == fields.Length - 1 ? ";" : ",");
        }

        builder.AppendLine();
    }

    private static Wall? FindWall(Room room, CardinalDirection direction) =>
        room.Walls.FirstOrDefault(wall => NormalizeDirection(wall.Orientation) == direction);

    private static CardinalDirection NormalizeDirection(CardinalDirection direction) =>
        direction switch
        {
            CardinalDirection.North or CardinalDirection.NorthEast or CardinalDirection.NorthWest => CardinalDirection.North,
            CardinalDirection.East or CardinalDirection.SouthEast => CardinalDirection.East,
            CardinalDirection.West or CardinalDirection.SouthWest => CardinalDirection.West,
            _ => CardinalDirection.South
        };

    private static string GetWallSurfaceName(string zoneName, CardinalDirection direction) =>
        $"{zoneName}_Wall_{direction}";

    private static string GetScheduleName(Room room, string loadType) =>
        $"{GetZoneName(room)}_{loadType}_Schedule";

    private static string GetZoneName(Room room) =>
        SafeName($"{room.Floor.Name}_{room.Name}");

    private static IEnumerable<Room> GetRooms(Building building) =>
        building.Floors
            .OrderBy(floor => floor.Id)
            .SelectMany(floor => floor.Rooms.OrderBy(room => room.Id));

    private static IReadOnlyList<RoomPlacement> CreateRoomPlacements(Building building)
    {
        var placements = new List<RoomPlacement>();
        var floorElevation = 0.0;

        foreach (var floor in building.Floors.OrderBy(floor => floor.Id))
        {
            var rooms = floor.Rooms.OrderBy(room => room.Id).ToArray();
            if (rooms.Length == 0)
                continue;

            var totalFloorArea = rooms.Sum(room => room.Area.SquareMeters);
            var maxRowWidth = Math.Max(12.0, Math.Sqrt(totalFloorArea) * 1.8);
            var commonDepth = Math.Max(3.0, Math.Sqrt(totalFloorArea / rooms.Length));
            var cursorX = 0.0;
            var cursorY = 0.0;
            var rowDepth = 0.0;
            var maxHeight = 0.0;

            foreach (var room in rooms)
            {
                var previewGeometry = RoomGeometry.Create(room, cursorX, cursorY, floorElevation, commonDepth);
                if (cursorX > 0 && cursorX + previewGeometry.Width > maxRowWidth)
                {
                    cursorX = 0;
                    cursorY += rowDepth;
                    rowDepth = 0;
                }

                var geometry = RoomGeometry.Create(room, cursorX, cursorY, floorElevation, commonDepth);
                placements.Add(new RoomPlacement(room, geometry));

                cursorX += geometry.Width;
                rowDepth = Math.Max(rowDepth, geometry.Depth);
                maxHeight = Math.Max(maxHeight, geometry.Height);
            }

            floorElevation += maxHeight + 0.5;
        }

        return placements;
    }

    private static string? FindAdjacentSurfaceName(
        RoomPlacement placement,
        CardinalDirection direction,
        IReadOnlyList<RoomPlacement> placements)
    {
        var geometry = placement.Geometry;
        foreach (var other in placements)
        {
            if (ReferenceEquals(other, placement))
                continue;

            var otherGeometry = other.Geometry;
            var otherDirection = Opposite(direction);
            if (AreAdjacent(geometry, otherGeometry, direction))
                return GetWallSurfaceName(GetZoneName(other.Room), otherDirection);
        }

        return null;
    }

    private static bool AreAdjacent(RoomGeometry first, RoomGeometry second, CardinalDirection direction)
    {
        const double tolerance = 0.001;
        if (Math.Abs(first.Z - second.Z) > tolerance || Math.Abs(first.Height - second.Height) > tolerance)
            return false;

        return direction switch
        {
            CardinalDirection.East =>
                NearlyEqual(first.X + first.Width, second.X) &&
                NearlyEqual(first.Y, second.Y) &&
                NearlyEqual(first.Depth, second.Depth),
            CardinalDirection.West =>
                NearlyEqual(first.X, second.X + second.Width) &&
                NearlyEqual(first.Y, second.Y) &&
                NearlyEqual(first.Depth, second.Depth),
            CardinalDirection.North =>
                NearlyEqual(first.Y + first.Depth, second.Y) &&
                NearlyEqual(first.X, second.X) &&
                NearlyEqual(first.Width, second.Width),
            CardinalDirection.South =>
                NearlyEqual(first.Y, second.Y + second.Depth) &&
                NearlyEqual(first.X, second.X) &&
                NearlyEqual(first.Width, second.Width),
            _ => false
        };

        static bool NearlyEqual(double left, double right) =>
            Math.Abs(left - right) <= tolerance;
    }

    private static CardinalDirection Opposite(CardinalDirection direction) =>
        direction switch
        {
            CardinalDirection.North => CardinalDirection.South,
            CardinalDirection.East => CardinalDirection.West,
            CardinalDirection.South => CardinalDirection.North,
            CardinalDirection.West => CardinalDirection.East,
            _ => direction
        };

    private static string FormatPoint(Point3d point) =>
        $"{F(point.X)}, {F(point.Y)}, {F(point.Z)}";

    private static string F(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string SafeName(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var c in name.Trim())
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        var safe = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(safe) ? "AE_Object" : safe;
    }

    private sealed class ConstructionRegistry
    {
        private readonly StringBuilder _builder;
        private readonly Dictionary<string, string> _wallConstructions = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _windowConstructions = new(StringComparer.OrdinalIgnoreCase);

        public ConstructionRegistry(StringBuilder builder) => _builder = builder;

        public string GetWallConstruction(Wall? wall)
        {
            var uValue = wall?.ConstructionAssembly?.UValueWPerM2K > 0
                ? wall.ConstructionAssembly.UValueWPerM2K
                : wall?.UValue.Value ?? 1.8;
            var key = F(uValue);

            if (_wallConstructions.TryGetValue(key, out var existing))
                return existing;

            var name = $"AE_Wall_U_{key.Replace('.', '_')}";
            var materialName = $"{name}_Material";
            AppendObject(_builder, "Material:NoMass", materialName, "Rough", F(1.0 / uValue));
            AppendObject(_builder, "Construction", name, materialName);

            _wallConstructions[key] = name;
            return name;
        }

        public string GetWindowConstruction(Window window)
        {
            var key = $"{F(window.UValue.Value)}_{F(window.Shgc.Value)}";
            if (_windowConstructions.TryGetValue(key, out var existing))
                return existing;

            var name = $"AE_Window_U_{F(window.UValue.Value).Replace('.', '_')}_SHGC_{F(window.Shgc.Value).Replace('.', '_')}";
            var materialName = $"{name}_Material";
            AppendObject(
                _builder,
                "WindowMaterial:SimpleGlazingSystem",
                materialName,
                F(window.UValue.Value),
                F(window.Shgc.Value),
                "0.6");
            AppendObject(_builder, "Construction", name, materialName);

            _windowConstructions[key] = name;
            return name;
        }
    }

    private sealed record Point3d(double X, double Y, double Z);

    private sealed record RoomPlacement(Room Room, RoomGeometry Geometry);

    private sealed class RoomGeometry
    {
        private RoomGeometry(double x, double y, double z, double width, double depth, double height)
        {
            X = x;
            Y = y;
            Z = z;
            Width = width;
            Depth = depth;
            Height = height;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double Width { get; }
        public double Depth { get; }
        public double Height { get; }

        public IReadOnlyList<Point3d> FloorVertices =>
        [
            new(X, Y, Z),
            new(X, Y + Depth, Z),
            new(X + Width, Y + Depth, Z),
            new(X + Width, Y, Z)
        ];

        public IReadOnlyList<Point3d> RoofVertices =>
        [
            new(X, Y + Depth, Z + Height),
            new(X, Y, Z + Height),
            new(X + Width, Y, Z + Height),
            new(X + Width, Y + Depth, Z + Height)
        ];

        public IReadOnlyList<Point3d> NorthWallVertices =>
        [
            new(X, Y + Depth, Z + Height),
            new(X, Y + Depth, Z),
            new(X + Width, Y + Depth, Z),
            new(X + Width, Y + Depth, Z + Height)
        ];

        public IReadOnlyList<Point3d> EastWallVertices =>
        [
            new(X + Width, Y + Depth, Z + Height),
            new(X + Width, Y + Depth, Z),
            new(X + Width, Y, Z),
            new(X + Width, Y, Z + Height)
        ];

        public IReadOnlyList<Point3d> SouthWallVertices =>
        [
            new(X + Width, Y, Z + Height),
            new(X + Width, Y, Z),
            new(X, Y, Z),
            new(X, Y, Z + Height)
        ];

        public IReadOnlyList<Point3d> WestWallVertices =>
        [
            new(X, Y, Z + Height),
            new(X, Y, Z),
            new(X, Y + Depth, Z),
            new(X, Y + Depth, Z + Height)
        ];

        public static RoomGeometry Create(Room room, double x, double y, double z, double? depthOverride = null)
        {
            var depth = Math.Max(1.0, depthOverride ?? Math.Sqrt(room.Area.SquareMeters / 1.2));
            var width = Math.Max(1.0, room.Area.SquareMeters / depth);
            var height = Math.Max(2.0, room.HeightM);
            return new RoomGeometry(x, y, z, width, depth, height);
        }

        public IReadOnlyList<Point3d> GetWindowVertices(
            CardinalDirection direction,
            double areaM2,
            int index,
            int count)
        {
            var wallLength = direction is CardinalDirection.North or CardinalDirection.South ? Width : Depth;
            var clearMargin = Math.Min(0.25, wallLength * 0.1);
            var availableLength = Math.Max(0.3, wallLength - 2 * clearMargin);
            var maxWidth = Math.Max(0.2, availableLength * 0.85 / Math.Max(1, count));
            var sillHeight = Math.Min(0.9, Height * 0.25);
            var headClearance = Math.Max(0.2, Height * 0.08);
            var maxHeight = Math.Max(0.2, Height - sillHeight - headClearance);
            var usableArea = Math.Min(areaM2, maxWidth * maxHeight);
            var windowWidth = Math.Clamp(Math.Sqrt(usableArea * 1.4), 0.2, maxWidth);
            var windowHeight = Math.Clamp(usableArea / Math.Max(windowWidth, 0.1), 0.2, maxHeight);
            windowWidth = Math.Clamp(usableArea / Math.Max(windowHeight, 0.1), 0.2, maxWidth);

            var spacing = wallLength / (count + 1);
            var center = spacing * (index + 1);
            var left = Math.Clamp(center - windowWidth / 2, clearMargin, wallLength - clearMargin - windowWidth);
            var right = left + windowWidth;
            var bottom = sillHeight;
            var top = bottom + windowHeight;

            return direction switch
            {
                CardinalDirection.North =>
                [
                    new(X + left, Y + Depth, Z + top),
                    new(X + left, Y + Depth, Z + bottom),
                    new(X + right, Y + Depth, Z + bottom),
                    new(X + right, Y + Depth, Z + top)
                ],
                CardinalDirection.East =>
                [
                    new(X + Width, Y + Depth - left, Z + top),
                    new(X + Width, Y + Depth - left, Z + bottom),
                    new(X + Width, Y + Depth - right, Z + bottom),
                    new(X + Width, Y + Depth - right, Z + top)
                ],
                CardinalDirection.West =>
                [
                    new(X, Y + left, Z + top),
                    new(X, Y + left, Z + bottom),
                    new(X, Y + right, Z + bottom),
                    new(X, Y + right, Z + top)
                ],
                _ =>
                [
                    new(X + Width - left, Y, Z + top),
                    new(X + Width - left, Y, Z + bottom),
                    new(X + Width - right, Y, Z + bottom),
                    new(X + Width - right, Y, Z + top)
                ]
            };
        }
    }
}
