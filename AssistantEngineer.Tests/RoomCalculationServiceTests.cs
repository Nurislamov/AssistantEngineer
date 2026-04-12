using AssistantEngineer.Models;
using AssistantEngineer.Services;

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
        
        var windows = Array.Empty<Window>();

        // Act
        var result = service.Calculate(room, windows);

        // Assert
        Assert.Equal(1, result.RoomId);
        Assert.Equal(2400, result.BaseRoomLoadW);
        Assert.Equal(0, result.TotalWindowAreaM2);
        Assert.Equal(0, result.WindowHeatGainW);
        Assert.Equal(2400, result.TotalHeatLoadW);
        Assert.Equal(2.4, result.TotalHeatLoadKw);
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
        var result = service.Calculate(room, windows);

        // Assert
        Assert.Equal(3.5, result.TotalWindowAreaM2);
        Assert.Equal(875, result.WindowHeatGainW);
        Assert.Equal(2400, result.BaseRoomLoadW);
        Assert.Equal(3275, result.TotalHeatLoadW);
        Assert.Equal(3.28, result.TotalHeatLoadKw);
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
        
        var windows = Array.Empty<Window>();

        // Act
        var lowResult = service.Calculate(roomLowDelta, windows);
        var highResult = service.Calculate(roomHighDelta, windows);

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
        var result = service.Calculate(room, []);

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
        var result = service.Calculate(room, []);

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
        double outdoorTemperatureC)
    {
        return new Room
        {
            Id = id,
            Name = $"Room {id}",
            AreaM2 = areaM2,
            HeightM = heightM,
            VolumeM3 = areaM2 * heightM,
            IndoorTemperatureC = indoorTemperatureC,
            OutdoorTemperatureC = outdoorTemperatureC
        };
    }
}
