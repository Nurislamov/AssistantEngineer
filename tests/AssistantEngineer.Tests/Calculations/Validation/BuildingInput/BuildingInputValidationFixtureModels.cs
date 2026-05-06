using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Validation.BuildingInput;

internal sealed record BuildingInputValidationFixture(
    string Id,
    string Scenario,
    BuildingInputValidationReadinessStatus ExpectedReadinessStatus,
    IReadOnlyList<string> ExpectedDiagnosticCodes,
    IReadOnlyList<string> ExpectedCorrectionIds);

internal static class BuildingInputValidationFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "building-input-validation");

    public static IReadOnlyList<BuildingInputValidationFixture> LoadAll()
    {
        if (!Directory.Exists(FixtureDirectory))
            throw new DirectoryNotFoundException($"Fixture directory was not found: {FixtureDirectory}");

        return Directory.GetFiles(FixtureDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(path => JsonSerializer.Deserialize<BuildingInputValidationFixture>(
                File.ReadAllText(path),
                SerializerOptions) ?? throw new InvalidOperationException($"Fixture did not parse: {path}"))
            .ToArray();
    }
}

internal static class BuildingInputValidationScenarioBuilder
{
    public static BuildingInputValidationRequest BuildRequest(string scenario) =>
        scenario switch
        {
            "valid-simple-room-ready" => new BuildingInputValidationRequest(BuildValidSimpleRoom()),
            "room-zero-area-blocked" => new BuildingInputValidationRequest(BuildRoomZeroArea()),
            "external-wall-missing-orientation-warning" => new BuildingInputValidationRequest(BuildExternalWallMissingOrientation()),
            "window-area-exceeds-wall-warning" => new BuildingInputValidationRequest(BuildWindowAreaExceedsWall()),
            "ground-contact-missing-metadata-warning" => new BuildingInputValidationRequest(BuildGroundBoundaryWithoutMetadata()),
            "construction-opt-in-missing-layers-warning" => new BuildingInputValidationRequest(
                BuildConstructionOptInWithoutLayers(),
                IsConstructionLayerMassOptInIntended: true),
            _ => throw new InvalidOperationException($"Unknown building input validation scenario: {scenario}")
        };

    public static Building BuildValidSimpleRoom()
    {
        var room = CreateBaseRoom(areaM2: 20.0, heightM: 3.0);
        var wall = room.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External);
        Assert.True(wall.IsSuccess);

        var window = room.AddWindow(
            Area.FromSquareMeters(2.0).Value,
            ThermalTransmittance.FromValue(1.5).Value,
            SolarHeatGainCoefficient.FromValue(0.6).Value,
            CardinalDirection.South);
        Assert.True(window.IsSuccess);

        return room.Floor.Building;
    }

    public static Building BuildRoomZeroArea()
    {
        var room = CreateBaseRoom(areaM2: 20.0, heightM: 3.0);
        SetInitOnlyDouble(room.Area, "<SquareMeters>k__BackingField", 0.0);
        return room.Floor.Building;
    }

    public static Building BuildExternalWallMissingOrientation()
    {
        var room = CreateBaseRoom(areaM2: 20.0, heightM: 3.0);
        var wall = room.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            (CardinalDirection)(-1),
            WallBoundaryType.External);
        Assert.True(wall.IsSuccess);
        return room.Floor.Building;
    }

    public static Building BuildWindowAreaExceedsWall()
    {
        var room = CreateBaseRoom(areaM2: 20.0, heightM: 3.0);
        var wall = room.AddWall(
            Area.FromSquareMeters(4.0).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External);
        Assert.True(wall.IsSuccess);

        var window = room.AddWindow(
            Area.FromSquareMeters(5.0).Value,
            ThermalTransmittance.FromValue(1.5).Value,
            SolarHeatGainCoefficient.FromValue(0.6).Value,
            CardinalDirection.South);
        Assert.True(window.IsSuccess);

        return room.Floor.Building;
    }

    public static Building BuildGroundBoundaryWithoutMetadata()
    {
        var room = CreateBaseRoom(areaM2: 20.0, heightM: 3.0);
        var wall = room.AddWall(
            Area.FromSquareMeters(8.0).Value,
            ThermalTransmittance.FromValue(0.5).Value,
            CardinalDirection.South,
            WallBoundaryType.Ground);
        Assert.True(wall.IsSuccess);
        return room.Floor.Building;
    }

    public static Building BuildConstructionOptInWithoutLayers()
    {
        var room = CreateBaseRoom(areaM2: 20.0, heightM: 3.0);
        var wall = room.AddWall(
            Area.FromSquareMeters(10.0).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External);
        Assert.True(wall.IsSuccess);
        return room.Floor.Building;
    }

    public static Building BuildInvalidWallUValue()
    {
        var room = CreateBaseRoom(areaM2: 20.0, heightM: 3.0);
        var wall = room.AddWall(
            Area.FromSquareMeters(10.0).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External);
        Assert.True(wall.IsSuccess);

        SetInitOnlyDouble(wall.Value.UValue, "<Value>k__BackingField", -0.1);
        return room.Floor.Building;
    }

    public static Building BuildInvalidShgcWindow()
    {
        var room = CreateBaseRoom(areaM2: 20.0, heightM: 3.0);
        var wall = room.AddWall(
            Area.FromSquareMeters(10.0).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External);
        Assert.True(wall.IsSuccess);

        var window = room.AddWindow(
            Area.FromSquareMeters(2.0).Value,
            ThermalTransmittance.FromValue(1.5).Value,
            SolarHeatGainCoefficient.FromValue(0.6).Value,
            CardinalDirection.South);
        Assert.True(window.IsSuccess);

        SetInitOnlyDouble(window.Value.Shgc, "<Value>k__BackingField", 1.2);
        return room.Floor.Building;
    }

    private static Room CreateBaseRoom(double areaM2, double heightM)
    {
        var project = Project.Create("Building input validation project").Value;
        var building = Building.Create("Building", project).Value;
        var addBuilding = project.AddBuilding(building);
        Assert.True(addBuilding.IsSuccess);

        var floorResult = building.AddFloor("Floor");
        Assert.True(floorResult.IsSuccess);

        var roomResult = floorResult.Value.AddRoom(
            name: "Room 1",
            area: Area.FromSquareMeters(areaM2).Value,
            heightM: heightM,
            indoorTemp: Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: null,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(300).Value,
            type: RoomType.Office);
        Assert.True(roomResult.IsSuccess);

        return roomResult.Value;
    }

    private static void SetInitOnlyDouble(object target, string backingFieldName, double value)
    {
        var field = target.GetType().GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }
}
