using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Models.Schedules;
using AssistantEngineer.Domain.Models.Ventilation;
using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;
using AssistantEngineer.Infrastructure.Services.Benchmarks;

namespace AssistantEngineer.Tests;

public class EnergyPlusModelExporterTests
{
    [Fact]
    public async Task ExportAsyncCreatesIdfWithZonesSurfacesLoadsAndIdealLoads()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var exporter = new EnergyPlusModelExporter();
            var building = CreateBuilding();
            var outputPath = Path.Combine(tempDirectory, "building.idf");

            var result = await exporter.ExportAsync(building, outputPath);

            Assert.True(result.IsSuccess);
            Assert.Equal(outputPath, result.Value.ModelPath);
            Assert.True(File.Exists(outputPath));

            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("Version,", content);
            Assert.Contains("Zone,", content);
            Assert.Contains("Office_101", content);
            Assert.Contains("BuildingSurface:Detailed,", content);
            Assert.Contains("FenestrationSurface:Detailed,", content);
            Assert.Contains("People,", content);
            Assert.Contains("ZoneInfiltration:DesignFlowRate,", content);
            Assert.Contains("ZoneHVAC:IdealLoadsAirSystem,", content);
            Assert.Contains("Zone Ideal Loads Supply Air Total Cooling Energy", content);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsyncReturnsValidationWhenOutputPathIsMissing()
    {
        var exporter = new EnergyPlusModelExporter();

        var result = await exporter.ExportAsync(CreateBuilding(), string.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("output path", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportAsyncReturnsValidationWhenBuildingHasNoRooms()
    {
        var exporter = new EnergyPlusModelExporter();
        var building = DomainInvariantTests.CreateBuilding();

        var result = await exporter.ExportAsync(
            building,
            Path.Combine(Path.GetTempPath(), "empty-building.idf"));

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("room", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportAsyncUsesFloorQualifiedZoneNamesForRepeatedRoomNames()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var exporter = new EnergyPlusModelExporter();
            var project = DomainInvariantTests.CreateProject("EnergyPlus project");
            var building = Building.Create("EnergyPlus building", project).Value;
            Assert.True(project.AddBuilding(building).IsSuccess);
            foreach (var floorName in new[] { "Level 1", "Level 2" })
            {
                var floor = building.AddFloor(floorName).Value;
                var room = floor.AddRoom(
                    "Office 101",
                    Area.FromSquareMeters(24).Value,
                    3,
                    Temperature.FromCelsius(22).Value,
                    Temperature.FromCelsius(34).Value).Value;
                Assert.True(room.AddWall(
                    Area.FromSquareMeters(14).Value,
                    isExternal: true,
                    ThermalTransmittance.FromValue(1.1).Value,
                    CardinalDirection.South).IsSuccess);
            }

            var outputPath = Path.Combine(tempDirectory, "building.idf");

            var result = await exporter.ExportAsync(building, outputPath);

            Assert.True(result.IsSuccess);
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("Level_1_Office_101", content);
            Assert.Contains("Level_2_Office_101", content);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsyncCreatesSurfaceBoundaryForAdjacentRooms()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var exporter = new EnergyPlusModelExporter();
            var project = DomainInvariantTests.CreateProject("EnergyPlus project");
            var building = Building.Create("EnergyPlus building", project).Value;
            Assert.True(project.AddBuilding(building).IsSuccess);
            var floor = building.AddFloor("Level 1").Value;
            var firstRoom = floor.AddRoom(
                "Room A",
                Area.FromSquareMeters(24).Value,
                3,
                Temperature.FromCelsius(22).Value,
                Temperature.FromCelsius(34).Value).Value;
            var secondRoom = floor.AddRoom(
                "Room B",
                Area.FromSquareMeters(24).Value,
                3,
                Temperature.FromCelsius(22).Value,
                Temperature.FromCelsius(34).Value).Value;
            Assert.True(firstRoom.AddWall(
                Area.FromSquareMeters(14).Value,
                isExternal: true,
                ThermalTransmittance.FromValue(1.1).Value,
                CardinalDirection.South).IsSuccess);
            Assert.True(secondRoom.AddWall(
                Area.FromSquareMeters(14).Value,
                isExternal: true,
                ThermalTransmittance.FromValue(1.1).Value,
                CardinalDirection.South).IsSuccess);
            var outputPath = Path.Combine(tempDirectory, "adjacent.idf");

            var result = await exporter.ExportAsync(building, outputPath);

            Assert.True(result.IsSuccess);
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("Level_1_Room_A_Wall_East", content);
            Assert.Contains("Level_1_Room_B_Wall_West", content);
            Assert.Contains("  Surface,", content);
            Assert.Contains("  Level_1_Room_B_Wall_West,", content);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static Building CreateBuilding()
    {
        var project = DomainInvariantTests.CreateProject("EnergyPlus project");
        var building = Building.Create("EnergyPlus building", project).Value;
        Assert.True(project.AddBuilding(building).IsSuccess);

        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Office 101",
            Area.FromSquareMeters(24).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(34).Value,
            peopleCount: 3,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(240).Value).Value;

        var occupiedSchedule = HourlySchedule.Create(
            "Occupied",
            Enumerable.Range(0, 24).Select(hour => hour is >= 8 and <= 18 ? 1.0 : 0.1).ToArray()).Value;
        Assert.True(room.SetOccupancySchedule(occupiedSchedule).IsSuccess);
        Assert.True(room.SetEquipmentSchedule(occupiedSchedule).IsSuccess);
        Assert.True(room.SetLightingSchedule(occupiedSchedule).IsSuccess);
        Assert.True(room.SetVentilationParameters(VentilationParameters.Create(0.7).Value).IsSuccess);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(14).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(1.1).Value,
            CardinalDirection.South).IsSuccess);
        Assert.True(room.AddWindow(
            Area.FromSquareMeters(4).Value,
            ThermalTransmittance.FromValue(2.2).Value,
            SolarHeatGainCoefficient.FromValue(0.45).Value,
            CardinalDirection.South).IsSuccess);

        return building;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"assistant-engineer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
