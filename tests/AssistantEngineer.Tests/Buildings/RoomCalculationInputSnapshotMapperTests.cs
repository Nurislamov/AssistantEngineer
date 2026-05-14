using System.Reflection;
using System.Text.Json;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Buildings;

public sealed class RoomCalculationInputSnapshotMapperTests
{
    [Fact]
    public void ToCalculationInputSnapshot_IsDeterministic_AndOrdersEnvelopeCollectionsById()
    {
        var room = CreateRoomGraph(withClimate: true, withVentilation: true, withSchedules: true, withGround: true);
        var wallA = room.Walls.First();
        var wallB = room.Walls.Last();
        var windowA = room.Windows.First();
        var windowB = room.Windows.Last();
        SetId(wallA, 42);
        SetId(wallB, 3);
        SetId(windowA, 17);
        SetId(windowB, 5);

        var snapshotA = room.ToCalculationInputSnapshot();
        var snapshotB = room.ToCalculationInputSnapshot();

        var jsonA = JsonSerializer.Serialize(snapshotA);
        var jsonB = JsonSerializer.Serialize(snapshotB);

        Assert.Equal(jsonA, jsonB);
        Assert.Equal([3, 42], snapshotA.Walls.Select(wall => wall.WallId).ToArray());
        Assert.Equal([5, 17], snapshotA.Windows.Select(window => window.WindowId).ToArray());
        Assert.Equal(["Wall", "Wall", "Window", "Window"], snapshotA.EnvelopeElements.Select(element => element.Kind).ToArray());
        Assert.Equal([3, 42, 5, 17], snapshotA.EnvelopeElements.Select(element => element.ElementId).ToArray());
    }

    [Fact]
    public void ToCalculationInputSnapshot_MapsNullOptionalInputs_WhenDataIsNotConfigured()
    {
        var room = CreateRoomGraph(withClimate: false, withVentilation: false, withSchedules: false, withGround: false);

        var snapshot = room.ToCalculationInputSnapshot();

        Assert.Null(snapshot.Climate);
        Assert.Null(snapshot.Ventilation);
        Assert.Null(snapshot.GroundBoundary);
        Assert.Null(snapshot.OccupancySchedule);
        Assert.Null(snapshot.EquipmentSchedule);
        Assert.Null(snapshot.LightingSchedule);
        Assert.Null(snapshot.OutdoorTemperatureOverrideC);
        Assert.Equal(2, snapshot.Walls.Count);
        Assert.Equal(2, snapshot.Windows.Count);
    }

    [Fact]
    public void ToCalculationInputSnapshot_PreservesScheduleFactorOrder()
    {
        var room = CreateRoomGraph(withClimate: true, withVentilation: true, withSchedules: true, withGround: false);
        var factors = Enumerable.Range(0, 24).Select(index => index / 23.0).ToArray();
        var schedule = HourlySchedule.Create("Ordered", factors).Value;
        SetId(schedule, 901);
        Assert.True(room.SetOccupancySchedule(schedule).IsSuccess);
        Assert.True(room.SetEquipmentSchedule(schedule).IsSuccess);
        Assert.True(room.SetLightingSchedule(schedule).IsSuccess);

        var snapshot = room.ToCalculationInputSnapshot();

        Assert.NotNull(snapshot.OccupancySchedule);
        Assert.Equal(901, snapshot.OccupancySchedule!.ScheduleId);
        Assert.Equal(factors, snapshot.OccupancySchedule.Factors);
        Assert.Equal(factors, snapshot.EquipmentSchedule!.Factors);
        Assert.Equal(factors, snapshot.LightingSchedule!.Factors);
    }

    private static Room CreateRoomGraph(
        bool withClimate,
        bool withVentilation,
        bool withSchedules,
        bool withGround)
    {
        var project = DomainInvariantTests.CreateProject("Snapshot mapping project");
        SetId(project, 700);

        ClimateZone? climate = null;
        if (withClimate)
        {
            climate = ClimateZone.Create(
                "Snapshot climate",
                Temperature.FromCelsius(35).Value,
                Temperature.FromCelsius(-5).Value).Value;
            SetId(climate, 800);
        }

        var building = Building.Create("Snapshot building", project, climate).Value;
        SetId(building, 701);
        Assert.True(project.AddBuilding(building).IsSuccess);

        var floor = building.AddFloor("Level 1").Value;
        SetId(floor, 702);

        var room = floor.AddRoom(
            "Snapshot room",
            Area.FromSquareMeters(30).Value,
            3.2,
            Temperature.FromCelsius(21).Value,
            outdoorTemperatureOverride: null,
            peopleCount: 3,
            equipmentLoad: Power.FromWatts(450).Value,
            lightingLoad: Power.FromWatts(180).Value,
            type: RoomType.Office).Value;
        SetId(room, 703);

        var adjacent = floor.AddRoom(
            "Adjacent room",
            Area.FromSquareMeters(10).Value,
            3.0,
            Temperature.FromCelsius(18).Value,
            outdoorTemperatureOverride: null,
            peopleCount: 0,
            type: RoomType.Corridor).Value;
        SetId(adjacent, 704);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(14).Value,
            ThermalTransmittance.FromValue(0.42).Value,
            CardinalDirection.North,
            WallBoundaryType.External).IsSuccess);
        Assert.True(room.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(1.9).Value,
            CardinalDirection.East,
            WallBoundaryType.AdjacentUnconditioned,
            adjacent).IsSuccess);

        Assert.True(room.AddWindow(
            Area.FromSquareMeters(3).Value,
            ThermalTransmittance.FromValue(1.2).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.North).IsSuccess);
        Assert.True(room.AddWindow(
            Area.FromSquareMeters(2).Value,
            ThermalTransmittance.FromValue(1.4).Value,
            SolarHeatGainCoefficient.FromValue(0.45).Value,
            CardinalDirection.NorthWest).IsSuccess);

        if (withVentilation)
        {
            var ventilation = VentilationParameters.Create(
                airChangesPerHour: 0.8,
                heatRecoveryEfficiency: 0.5,
                infiltrationAirChangesPerHour: 0.2,
                windExposureFactor: 0.7,
                stackCoefficient: 0.2,
                windCoefficient: 0.1).Value;
            SetId(ventilation, 705);
            Assert.True(room.SetVentilationParameters(ventilation).IsSuccess);
        }

        if (withSchedules)
        {
            var schedule = HourlySchedule.Create(
                "Profile",
                Enumerable.Repeat(0.6, 24).ToArray()).Value;
            SetId(schedule, 706);
            Assert.True(room.SetOccupancySchedule(schedule).IsSuccess);
            Assert.True(room.SetEquipmentSchedule(schedule).IsSuccess);
            Assert.True(room.SetLightingSchedule(schedule).IsSuccess);
        }

        if (withGround)
        {
            var ground = GroundContactMetadata.Create(
                GroundContactType.SlabOnGround,
                exposedPerimeterM: 20,
                burialDepthM: 0,
                wallHeightBelowGradeM: 0,
                horizontalInsulationWidthM: 0.5,
                perimeterInsulationDepthM: 0.4,
                underfloorVentilationAirChangesPerHour: 0).Value;
            Assert.True(room.SetGroundContactMetadata(ground).IsSuccess);
        }

        return room;
    }

    private static void SetId(object entity, int id)
    {
        var field = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, id);
    }
}
