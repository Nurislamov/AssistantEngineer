using AssistantEngineer.Models;

namespace AssistantEngineer.Services;

public class RoomCalculationService
{
    public RoomCalculationResult Calculate(Room room)
    {
        const double baseCoolingLoadWPerM2  = 100.0;

        var deltaTemperatureC = 
            Math.Abs(room.OutdoorTemperatureC - room.IndoorTemperatureC);

        var heightAdjustmentFactor  = 
            room.HeightM > 0 ? room.HeightM / 3.0 : 1.0;
        
        var temperatureAdjustmentFactor  = 
            1.0 + (deltaTemperatureC * 0.02);

        var coolingLoadW  = 
            room.AreaM2 * baseCoolingLoadWPerM2  * heightAdjustmentFactor  * temperatureAdjustmentFactor ;

        return new RoomCalculationResult
        {
            RoomId = room.Id,
            HeatLoadWatts = Math.Round(coolingLoadW , 2),
            HeatLoadKilowatts = Math.Round(coolingLoadW  / 1000.0, 2),
            DeltaTemperature = deltaTemperatureC,
            BaseLoadPerSquareMeter = baseCoolingLoadWPerM2 ,
            HeightFactor = Math.Round(heightAdjustmentFactor , 2),
            TemperatureFactor = Math.Round(temperatureAdjustmentFactor , 2)
        };
    }
}