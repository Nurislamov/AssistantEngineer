using AssistantEngineer.Models;
using AssistantEngineer.Services;

namespace AssistantEngineer.Tests;

public class RoomCalculationServiceTests
{
    [Fact]
    public void Calculate_WithoutWindows_ReturnsBaseRoomLoadOnly()
    {
        // Arrange
        var service = new RoomCalculationService();

        var room = new Room
        {
            Id = 1,
            Name = "Office 101",
            AreaM2 = 20,
            HeightM = 3,
            VolumeM3 = 60,
            IndoorTemperatureC = 24,
            OutdoorTemperatureC = 34
        };

        var windows = new List<Window>();

        // Act
        var result = service.Calculate(room, windows);

        // Assert
        Assert.Equal(1, result.RoomId);
        Assert.Equal(2000, result.BaseRoomLoadW / result.TemperatureAdjustmentFactor); // косвенная проверка базы
        Assert.Equal(0, result.TotalWindowAreaM2);
        Assert.Equal(0, result.WindowHeatGainW);
        Assert.True(result.TotalHeatLoadW > 0);
        Assert.Equal(Math.Round(result.TotalHeatLoadW / 1000.0, 2), result.TotalHeatLoadKw);
    }

    [Fact]
    public void Calculate_WithWindows_AddsWindowHeatGain()
    {
        // Arrange
        var service = new RoomCalculationService();

        var room = new Room
        {
            Id = 1,
            Name = "Office 101",
            AreaM2 = 20,
            HeightM = 3,
            VolumeM3 = 60,
            IndoorTemperatureC = 24,
            OutdoorTemperatureC = 34
        };

        var windows = new List<Window>
        {
            new() { Id = 1, RoomId = 1, AreaM2 = 2.0 },
            new() { Id = 2, RoomId = 1, AreaM2 = 1.5 }
        };

        // Act
        var result = service.Calculate(room, windows);

        // Assert
        Assert.Equal(3.5, result.TotalWindowAreaM2);
        Assert.Equal(875, result.WindowHeatGainW); // 3.5 * 250
        Assert.True(result.TotalHeatLoadW > result.BaseRoomLoadW);
    }

    [Fact]
    public void Calculate_WithHigherTemperatureDifference_IncreasesLoad()
    {
        // Arrange
        var service = new RoomCalculationService();

        var roomLowDelta = new Room
        {
            Id = 1,
            Name = "Room A",
            AreaM2 = 20,
            HeightM = 3,
            VolumeM3 = 60,
            IndoorTemperatureC = 24,
            OutdoorTemperatureC = 30
        };

        var roomHighDelta = new Room
        {
            Id = 2,
            Name = "Room B",
            AreaM2 = 20,
            HeightM = 3,
            VolumeM3 = 60,
            IndoorTemperatureC = 24,
            OutdoorTemperatureC = 40
        };

        var windows = new List<Window>();

        // Act
        var lowResult = service.Calculate(roomLowDelta, windows);
        var highResult = service.Calculate(roomHighDelta, windows);

        // Assert
        Assert.True(highResult.TotalHeatLoadW > lowResult.TotalHeatLoadW);
        Assert.True(highResult.TemperatureAdjustmentFactor > lowResult.TemperatureAdjustmentFactor);
    }
}