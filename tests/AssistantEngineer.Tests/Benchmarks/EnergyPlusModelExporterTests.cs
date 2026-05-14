using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using AssistantEngineer.Infrastructure.Integrations.Benchmarks;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AssistantEngineer.Tests;

public class EnergyPlusModelExporterTests
{
    [Fact]
    public async Task ExportAsyncCreatesIdfWithZonesSurfacesLoadsAndIdealLoads()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifacts = CreateArtifactStore(tempDirectory);
            var exporter = new EnergyPlusModelExporter(artifacts);
            var building = CreateBuilding();

            var result = await exporter.ExportAsync(building, "building");

            Assert.True(result.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(result.Value.ModelArtifactId));
            var artifact = artifacts.GetModelArtifact(result.Value.ModelArtifactId);
            Assert.True(artifact.IsSuccess, artifact.Error);
            Assert.True(File.Exists(artifact.Value.FileSystemPath));

            var content = await File.ReadAllTextAsync(artifact.Value.FileSystemPath);
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
    public async Task ExportAsyncReturnsValidationWhenBuildingHasNoRooms()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var exporter = new EnergyPlusModelExporter(CreateArtifactStore(tempDirectory));
            var building = DomainInvariantTests.CreateBuilding();

            var result = await exporter.ExportAsync(building);

            Assert.True(result.IsFailure);
            Assert.Equal(ResultErrorType.Validation, result.ErrorType);
            Assert.Contains("room", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsyncUsesFloorQualifiedZoneNamesForRepeatedRoomNames()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifacts = CreateArtifactStore(tempDirectory);
            var exporter = new EnergyPlusModelExporter(artifacts);
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

            var result = await exporter.ExportAsync(building, "building");

            Assert.True(result.IsSuccess);
            var artifact = artifacts.GetModelArtifact(result.Value.ModelArtifactId);
            Assert.True(artifact.IsSuccess, artifact.Error);
            var content = await File.ReadAllTextAsync(artifact.Value.FileSystemPath);
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
            var artifacts = CreateArtifactStore(tempDirectory);
            var exporter = new EnergyPlusModelExporter(artifacts);
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
            var result = await exporter.ExportAsync(building, "adjacent");

            Assert.True(result.IsSuccess);
            var artifact = artifacts.GetModelArtifact(result.Value.ModelArtifactId);
            Assert.True(artifact.IsSuccess, artifact.Error);
            var content = await File.ReadAllTextAsync(artifact.Value.FileSystemPath);
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

    [Fact]
    public async Task ExportAsyncGeneratedModelIsDeterministicForSameBuilding()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifacts = CreateArtifactStore(tempDirectory);
            var exporter = new EnergyPlusModelExporter(artifacts);
            var building = CreateBuilding();

            var firstExport = await exporter.ExportAsync(building, "deterministic-1");
            var secondExport = await exporter.ExportAsync(building, "deterministic-2");

            Assert.True(firstExport.IsSuccess, firstExport.Error);
            Assert.True(secondExport.IsSuccess, secondExport.Error);

            var firstArtifact = artifacts.GetModelArtifact(firstExport.Value.ModelArtifactId);
            var secondArtifact = artifacts.GetModelArtifact(secondExport.Value.ModelArtifactId);
            Assert.True(firstArtifact.IsSuccess, firstArtifact.Error);
            Assert.True(secondArtifact.IsSuccess, secondArtifact.Error);

            var firstContent = await File.ReadAllTextAsync(firstArtifact.Value.FileSystemPath);
            var secondContent = await File.ReadAllTextAsync(secondArtifact.Value.FileSystemPath);

            Assert.Equal(firstContent, secondContent);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsyncGeneratedModelKeepsCriticalSectionOrderingAndBaselineHash()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var artifacts = CreateArtifactStore(tempDirectory);
            var exporter = new EnergyPlusModelExporter(artifacts);
            var building = CreateBuilding();

            var export = await exporter.ExportAsync(building, "ordering");
            Assert.True(export.IsSuccess, export.Error);

            var artifact = artifacts.GetModelArtifact(export.Value.ModelArtifactId);
            Assert.True(artifact.IsSuccess, artifact.Error);

            var content = await File.ReadAllTextAsync(artifact.Value.FileSystemPath);

            var versionIndex = content.IndexOf("Version,", StringComparison.Ordinal);
            var scheduleIndex = content.IndexOf("Schedule:Compact,", StringComparison.Ordinal);
            var zoneIndex = content.IndexOf("Zone,", StringComparison.Ordinal);
            var surfaceIndex = content.IndexOf("BuildingSurface:Detailed,", StringComparison.Ordinal);
            var windowIndex = content.IndexOf("FenestrationSurface:Detailed,", StringComparison.Ordinal);
            var peopleIndex = content.IndexOf("People,", StringComparison.Ordinal);
            var ventilationIndex = content.IndexOf("ZoneInfiltration:DesignFlowRate,", StringComparison.Ordinal);
            var idealLoadsIndex = content.IndexOf("ZoneHVAC:IdealLoadsAirSystem,", StringComparison.Ordinal);
            var outputIndex = content.IndexOf("Output:Variable,", StringComparison.Ordinal);

            Assert.True(versionIndex >= 0);
            Assert.True(scheduleIndex > versionIndex);
            Assert.True(zoneIndex > scheduleIndex);
            Assert.True(surfaceIndex > zoneIndex);
            Assert.True(windowIndex > surfaceIndex);
            Assert.True(peopleIndex > windowIndex);
            Assert.True(ventilationIndex > peopleIndex);
            Assert.True(idealLoadsIndex > ventilationIndex);
            Assert.True(outputIndex > idealLoadsIndex);

            var hash = ComputeSha256(content);
            Assert.Equal(64, hash.Length);
            Assert.StartsWith("9BF679500B6C09AC80C98DE647FFB62D0FDDF0785", hash, StringComparison.Ordinal);
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

    private static LocalEnergyPlusArtifactStore CreateArtifactStore(string rootDirectory) =>
        new(Options.Create(new EnergyPlusBenchmarkOptions { ArtifactRootDirectory = rootDirectory }));

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
