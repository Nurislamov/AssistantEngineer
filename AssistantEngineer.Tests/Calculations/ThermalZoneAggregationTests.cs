using System.Reflection;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class ThermalZoneAggregationTests
{
    [Fact]
    public async Task Iso52016BuildingCalculationGroupsRoomsByThermalZones()
    {
        var project = DomainInvariantTests.CreateProject();
        var building = Building.Create("Building", project).Value;
        var floor = building.AddFloor("Level 1").Value;
        var northRoom = floor.AddRoom(
            "North room",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value,
            peopleCount: 1).Value;
        var southRoom = floor.AddRoom(
            "South room",
            Area.FromSquareMeters(30).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value,
            peopleCount: 2).Value;
        SetEntityId(northRoom, 101);
        SetEntityId(southRoom, 102);

        Assert.True(building.AddThermalZone("North zone", [northRoom]).IsSuccess);
        Assert.True(building.AddThermalZone("South zone", [southRoom]).IsSuccess);

        var roomCalculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var aggregateCalculator = CalculationTestFactory.CreateAggregateCalculator(roomCalculator);

        var result = await aggregateCalculator.CalculateBuildingAsync(
            building,
            CoolingLoadCalculationMethod.Iso52016);

        Assert.Equal(2, result.ThermalZones.Count);
        Assert.Contains(result.ThermalZones, zone => zone.ThermalZoneName == "North zone" && zone.RoomIds.SequenceEqual([101]));
        Assert.Contains(result.ThermalZones, zone => zone.ThermalZoneName == "South zone" && zone.RoomIds.SequenceEqual([102]));
        Assert.Equal(
            result.ThermalZones.Sum(zone => zone.HourlyHeatLoadW[result.PeakHour!.Value]),
            result.TotalHeatLoadW,
            precision: 2);
    }

    [Fact]
    public void BuildingRejectsThermalZoneWithUnsavedRoomAlreadyAssignedToAnotherZone()
    {
        var building = DomainInvariantTests.CreateBuilding();
        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Room",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value).Value;
        Assert.Equal(0, room.Id);
        Assert.True(building.AddThermalZone("Zone 1", [room]).IsSuccess);

        var result = building.AddThermalZone("Zone 2", [room]);

        Assert.True(result.IsFailure);
    }

    private static void SetEntityId(object entity, int id)
    {
        var field = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, id);
    }
}
