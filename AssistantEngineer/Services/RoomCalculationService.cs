using AssistantEngineer.Models;

namespace AssistantEngineer.Services;

public class RoomCalculationService
{
    public RoomCalculationResult Calculate(Room room)
    {
        const double baseLoadPerSquareMeter = 100.0;

        var deltaTemperature = Math.Abs(room.OutdoorTemperature - room.IndoorTemperature);

        var heightFactor = room.Height > 0 ? room.Height / 3.0 : 1.0;
        var temperatureFactor = 1.0 + (deltaTemperature * 0.02);

        var heatLoadWatts = room.Area * baseLoadPerSquareMeter * heightFactor * temperatureFactor;

        return new RoomCalculationResult
        {
            RoomId = room.Id,
            HeatLoadWatts = Math.Round(heatLoadWatts, 2),
            HeatLoadKilowatts = Math.Round(heatLoadWatts / 1000.0, 2),
            DeltaTemperature = deltaTemperature,
            BaseLoadPerSquareMeter = baseLoadPerSquareMeter,
            HeightFactor = Math.Round(heightFactor, 2),
            TemperatureFactor = Math.Round(temperatureFactor, 2)
        };
    }
}