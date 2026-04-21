using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;

internal static class CoolingLoadResultFactory
{
    public static RoomCalculationResult Create(
        Room room,
        CoolingLoadCalculationMethod method,
        int peakHour,
        List<double> hourlyHeatLoadW,
        double baseLoad,
        double windowGain,
        double wallGain,
        double peopleGain,
        double equipmentGain,
        double lightingGain,
        double totalLoad,
        double deltaT,
        double heightAdjustmentFactor,
        double temperatureAdjustmentFactor,
        double reserveFactor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var designLoad = totalLoad * reserveFactor;
        var internalGain = peopleGain + equipmentGain + lightingGain;
        var totalWindowArea = room.Windows.Sum(window => window.Area.SquareMeters);
        var totalWallArea = room.Walls.Sum(wall => wall.Area.SquareMeters);
        var externalWallArea = room.Walls
            .Where(wall => wall.IsExternal)
            .Sum(wall => wall.Area.SquareMeters);

        return new RoomCalculationResult
        {
            RoomId = room.Id,
            RoomName = room.Name,
            CalculationMethod = method.ToString(),
            PeakHour = peakHour,
            AreaM2 = room.Area.SquareMeters,
            HeightM = room.HeightM,
            VolumeM3 = room.CalculateVolume(),
            IndoorTemperatureC = room.IndoorTemperature.Celsius,
            OutdoorTemperatureC = room.OutdoorTemperature.Celsius,
            PeopleCount = room.PeopleCount,
            EquipmentLoadW = room.EquipmentLoad.Watts,
            LightingLoadW = room.LightingLoad.Watts,
            TotalWindowAreaM2 = totalWindowArea,
            TotalWallAreaM2 = totalWallArea,
            ExternalWallAreaM2 = externalWallArea,
            BaseRoomLoadW = Round(baseLoad),
            WindowHeatGainW = Round(windowGain),
            WallHeatGainW = Round(wallGain),
            PeopleHeatGainW = Round(peopleGain),
            EquipmentHeatGainW = Round(equipmentGain),
            LightingHeatGainW = Round(lightingGain),
            InternalHeatGainW = Round(internalGain),
            TotalHeatLoadW = Round(totalLoad),
            TotalHeatLoadKw = Round(totalLoad / 1000.0),
            DeltaTemperatureC = Round(deltaT),
            HeightAdjustmentFactor = Round(heightAdjustmentFactor),
            TemperatureAdjustmentFactor = Round(temperatureAdjustmentFactor),
            DesignReserveFactor = reserveFactor,
            DesignCapacityW = Round(designLoad),
            DesignCapacityKw = Round(designLoad / 1000.0),
            HourlyHeatLoadW = hourlyHeatLoadW
        };
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
