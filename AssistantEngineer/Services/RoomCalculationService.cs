using AssistantEngineer.Models;

namespace AssistantEngineer.Services;

public class RoomCalculationService
{
    public RoomCalculationResult Calculate(Room room, IEnumerable<Window> windows)
    {
        const double baseCoolingLoadWPerM2 = 100.0;
        const double windowCoolingLoadWPerM2 = 250.0;

        var deltaTemperatureC =
            Math.Abs(room.OutdoorTemperatureC - room.IndoorTemperatureC);

        var heightAdjustmentFactor =
            room.HeightM > 0 ? room.HeightM / 3.0 : 1.0;

        var temperatureAdjustmentFactor =
            1.0 + (deltaTemperatureC * 0.02);

        var baseRoomLoadW =
            room.AreaM2 * baseCoolingLoadWPerM2 * heightAdjustmentFactor * temperatureAdjustmentFactor;

        var totalWindowAreaM2 = windows.Sum(w => w.AreaM2);

        var windowHeatGainW =
            totalWindowAreaM2 * windowCoolingLoadWPerM2;

        var totalHeatLoadW = baseRoomLoadW + windowHeatGainW;

        return new RoomCalculationResult
        {
            RoomId = room.Id,
            BaseRoomLoadW = Math.Round(baseRoomLoadW, 2),
            TotalWindowAreaM2 = Math.Round(totalWindowAreaM2, 2),
            WindowHeatGainW = Math.Round(windowHeatGainW, 2),
            TotalHeatLoadW = Math.Round(totalHeatLoadW, 2),
            TotalHeatLoadKw = Math.Round(totalHeatLoadW / 1000.0, 2),
            DeltaTemperatureC = deltaTemperatureC,
            HeightAdjustmentFactor = Math.Round(heightAdjustmentFactor, 2),
            TemperatureAdjustmentFactor = Math.Round(temperatureAdjustmentFactor, 2)
        };
    }
}