using AssistantEngineer.Models;
using AssistantEngineer.Services.Calculations;

namespace AssistantEngineer.Tests;

public class RoomCalculationServiceTests
{
    [Fact]
    public void Calculate_WithoutWindows_ReturnsExpectedBaseRoomLoad()
    {
        // Arrange
        var service = new RoomCalculationService();
        var room = CreateRoom(
            id: 1, areaM2: 20, 
            heightM: 3, 
            indoorTemperatureC: 24, 
            outdoorTemperatureC: 34);

        // Act
        var result = service.Calculate(room, windows:[], walls:[]);

        // Assert
        Assert.Equal(1, result.RoomId);
        Assert.Equal(2400, result.BaseRoomLoadW);
        Assert.Equal(0, result.TotalWindowAreaM2);
        Assert.Equal(0, result.WindowHeatGainW);
        Assert.Equal(0, result.TotalWallAreaM2);
        Assert.Equal(0, result.ExternalWallAreaM2);
        Assert.Equal(0, result.WallHeatGainW);
        Assert.Equal(0, result.PeopleHeatGainW);
        Assert.Equal(0, result.EquipmentHeatGainW);
        Assert.Equal(0, result.LightingHeatGainW);
        Assert.Equal(0, result.InternalHeatGainW);
        Assert.Equal(2400, result.TotalHeatLoadW);
        Assert.Equal(2.4, result.TotalHeatLoadKw);
        Assert.Equal(1.1, result.DesignReserveFactor);
        Assert.Equal(2640, result.DesignCapacityW);
        Assert.Equal(2.64, result.DesignCapacityKw);
        Assert.Equal(10, result.DeltaTemperatureC);
        Assert.Equal(1, result.HeightAdjustmentFactor);
        Assert.Equal(1.2, result.TemperatureAdjustmentFactor);
    }

    [Fact]
    public void Calculate_WithWindows_ReturnsExpectedTotalHeatLoad()
    {
        // Arrange
        var service = new RoomCalculationService();
        var room = CreateRoom(
            id: 1, 
            areaM2: 20, 
            heightM: 3, 
            indoorTemperatureC: 24, 
            outdoorTemperatureC: 34);

        var windows = new List<Window>
        {
            new() { Id = 1, RoomId = 1, AreaM2 = 2.0 },
            new() { Id = 2, RoomId = 1, AreaM2 = 1.5 }
        };
        
        // Act
        var result = service.Calculate(room, windows, walls:[]);

        // Assert
        Assert.Equal(3.5, result.TotalWindowAreaM2);
        Assert.Equal(875, result.WindowHeatGainW);
        Assert.Equal(2400, result.BaseRoomLoadW);
        Assert.Equal(3275, result.TotalHeatLoadW);
        Assert.Equal(3.28, result.TotalHeatLoadKw);
        Assert.Equal(3602.5, result.DesignCapacityW);
        Assert.Equal(3.6, result.DesignCapacityKw);
    }

    [Fact]
    public void Calculate_WithExternalWalls_ReturnsExpectedWallHeatGain()
    {
        // Arrange
        var service = new RoomCalculationService();
        var room = CreateRoom(
            id: 1,
            areaM2: 20,
            heightM: 3,
            indoorTemperatureC: 24,
            outdoorTemperatureC: 34);

        var walls = new List<Wall>
        {
            new() { Id = 1, RoomId = 1, AreaM2 = 12, IsExternal = true },
            new() { Id = 2, RoomId = 1, AreaM2 = 8, IsExternal = true }
        };

        // Act
        var result = service.Calculate(room, windows:[], walls);

        // Assert
        Assert.Equal(20, result.TotalWallAreaM2);
        Assert.Equal(20, result.ExternalWallAreaM2);
        Assert.Equal(1200, result.WallHeatGainW);
        Assert.Equal(2400, result.BaseRoomLoadW);
        Assert.Equal(3600, result.TotalHeatLoadW);
        Assert.Equal(3.6, result.TotalHeatLoadKw);
        Assert.Equal(3960, result.DesignCapacityW);
        Assert.Equal(3.96, result.DesignCapacityKw);
    }

    [Fact]
    public void Calculate_WithInternalWalls_IncludesWallAreaWithoutIncreasingHeatLoad()
    {
        // Arrange
        var service = new RoomCalculationService();
        var room = CreateRoom(
            id: 1,
            areaM2: 20,
            heightM: 3,
            indoorTemperatureC: 24,
            outdoorTemperatureC: 34);

        var walls = new List<Wall>
        {
            new() { Id = 1, RoomId = 1, AreaM2 = 12, IsExternal = false },
            new() { Id = 2, RoomId = 1, AreaM2 = 8, IsExternal = false }
        };

        // Act
        var result = service.Calculate(room, windows:[], walls);

        // Assert
        Assert.Equal(20, result.TotalWallAreaM2);
        Assert.Equal(0, result.ExternalWallAreaM2);
        Assert.Equal(0, result.WallHeatGainW);
        Assert.Equal(2400, result.BaseRoomLoadW);
        Assert.Equal(2400, result.TotalHeatLoadW);
        Assert.Equal(2.4, result.TotalHeatLoadKw);
        Assert.Equal(2640, result.DesignCapacityW);
        Assert.Equal(2.64, result.DesignCapacityKw);
    }

    [Fact]
    public void Calculate_WithInternalHeatGains_ReturnsExpectedInternalHeatGain()
    {
        // Arrange
        var service = new RoomCalculationService();
        var room = CreateRoom(
            id: 1,
            areaM2: 20,
            heightM: 3,
            indoorTemperatureC: 24,
            outdoorTemperatureC: 34,
            peopleCount: 3,
            equipmentLoadW: 500,
            lightingLoadW: 200);

        // Act
        var result = service.Calculate(room, windows:[], walls:[]);

        // Assert
        Assert.Equal(390, result.PeopleHeatGainW);
        Assert.Equal(500, result.EquipmentHeatGainW);
        Assert.Equal(200, result.LightingHeatGainW);
        Assert.Equal(1090, result.InternalHeatGainW);
        Assert.Equal(2400, result.BaseRoomLoadW);
        Assert.Equal(3490, result.TotalHeatLoadW);
        Assert.Equal(3.49, result.TotalHeatLoadKw);
        Assert.Equal(3839, result.DesignCapacityW);
        Assert.Equal(3.84, result.DesignCapacityKw);
    }

    [Fact]
    public void Calculate_WithWindowsWallsAndInternalHeatGains_ReturnsExpectedTotalHeatLoad()
    {
        // Arrange
        var service = new RoomCalculationService();
        var room = CreateRoom(
            id: 1,
            areaM2: 10,
            heightM: 3,
            indoorTemperatureC: 24,
            outdoorTemperatureC: 24,
            peopleCount: 1,
            equipmentLoadW: 70,
            lightingLoadW: 30);

        var windows = new List<Window>
        {
            new() { Id = 1, RoomId = 1, AreaM2 = 2 }
        };

        var walls = new List<Wall>
        {
            new() { Id = 1, RoomId = 1, AreaM2 = 10, IsExternal = true },
            new() { Id = 2, RoomId = 1, AreaM2 = 5, IsExternal = false }
        };

        // Act
        var result = service.Calculate(room, windows, walls);

        // Assert
        Assert.Equal(1000, result.BaseRoomLoadW);
        Assert.Equal(2, result.TotalWindowAreaM2);
        Assert.Equal(500, result.WindowHeatGainW);
        Assert.Equal(15, result.TotalWallAreaM2);
        Assert.Equal(10, result.ExternalWallAreaM2);
        Assert.Equal(600, result.WallHeatGainW);
        Assert.Equal(230, result.InternalHeatGainW);
        Assert.Equal(2330, result.TotalHeatLoadW);
        Assert.Equal(2.33, result.TotalHeatLoadKw);
        Assert.Equal(2563, result.DesignCapacityW);
        Assert.Equal(2.56, result.DesignCapacityKw);
    }

    [Fact]
    public void Calculate_WithHigherTemperatureDifference_IncreasesLoad()
    {
        // Arrange
        var service = new RoomCalculationService();
        var roomLowDelta = CreateRoom(
            id: 1, 
            areaM2: 20, 
            heightM: 3, 
            indoorTemperatureC: 24, 
            outdoorTemperatureC: 30);
        
        var roomHighDelta = CreateRoom(
            id: 2, 
            areaM2: 20, 
            heightM: 3, 
            indoorTemperatureC: 24, 
            outdoorTemperatureC: 40);

        // Act
        var lowResult = service.Calculate(roomLowDelta, windows:[], walls:[]);
        var highResult = service.Calculate(roomHighDelta, windows:[], walls:[]);

        // Assert
        Assert.Equal(2240, lowResult.TotalHeatLoadW);
        Assert.Equal(2640, highResult.TotalHeatLoadW);
        Assert.True(highResult.TotalHeatLoadW > lowResult.TotalHeatLoadW);
        Assert.True(highResult.TemperatureAdjustmentFactor > lowResult.TemperatureAdjustmentFactor);
    }

    [Fact]
    public void Calculate_WithHigherCeiling_IncreasesHeightAdjustmentFactor()
    {
        // Arrange
        var service = new RoomCalculationService();
        var room = CreateRoom(
            id: 1, 
            areaM2: 20, 
            heightM: 6, 
            indoorTemperatureC: 24, 
            outdoorTemperatureC: 24);

        // Act
        var result = service.Calculate(room, windows:[], walls:[]);

        // Assert
        Assert.Equal(2, result.HeightAdjustmentFactor);
        Assert.Equal(1, result.TemperatureAdjustmentFactor);
        Assert.Equal(0, result.DeltaTemperatureC);
        Assert.Equal(4000, result.BaseRoomLoadW);
        Assert.Equal(4000, result.TotalHeatLoadW);
        Assert.Equal(4, result.TotalHeatLoadKw);
    }

    [Fact]
    public void Calculate_WithSameIndoorAndOutdoorTemperature_DoesNotApplyTemperatureIncrease()
    {
        // Arrange
        var service = new RoomCalculationService();
        var room = CreateRoom(
            id: 1, 
            areaM2: 20, 
            heightM: 3, 
            indoorTemperatureC: 24, 
            outdoorTemperatureC: 24);

        // Act
        var result = service.Calculate(room, windows:[], walls:[]);

        // Assert
        Assert.Equal(0, result.DeltaTemperatureC);
        Assert.Equal(1, result.TemperatureAdjustmentFactor);
        Assert.Equal(2000, result.BaseRoomLoadW);
        Assert.Equal(2000, result.TotalHeatLoadW);
        Assert.Equal(2, result.TotalHeatLoadKw);
    }

    private static Room CreateRoom(
        int id,
        double areaM2,
        double heightM,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        int peopleCount = 0,
        double equipmentLoadW = 0,
        double lightingLoadW = 0)
    {
        return new Room
        {
            Id = id,
            Name = $"Room {id}",
            AreaM2 = areaM2,
            HeightM = heightM,
            VolumeM3 = areaM2 * heightM,
            IndoorTemperatureC = indoorTemperatureC,
            OutdoorTemperatureC = outdoorTemperatureC,
            PeopleCount = peopleCount,
            EquipmentLoadW = equipmentLoadW,
            LightingLoadW = lightingLoadW
        };
    }
}
