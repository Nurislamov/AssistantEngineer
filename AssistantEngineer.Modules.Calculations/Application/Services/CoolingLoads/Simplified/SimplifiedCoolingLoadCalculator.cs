using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Simplified;

public sealed class SimplifiedCoolingLoadCalculator : IRoomCoolingLoadCalculationStrategy
{
    private readonly CoolingLoadCalculationOptions _options;
    private readonly ICoolingLoadReferenceData _referenceData;

    public SimplifiedCoolingLoadCalculator(
        CoolingLoadCalculationOptions options,
        ICoolingLoadReferenceData referenceData)
    {
        _options = options;
        _referenceData = referenceData;
    }

    public CoolingLoadCalculationMethod Method => CoolingLoadCalculationMethod.Simplified;

    public Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reserveFactor = GetReserveFactor(preferences);
        var deltaT = Math.Abs(room.OutdoorTemperature.Celsius - room.IndoorTemperature.Celsius);
        var baseLoad = room.CalculateVolume() * _options.SimplifiedVolumeLoadWPerM3 * GetRoomTypeLoadFactor(room.Type);
        var windowGain = room.Windows.Sum(window =>
            window.Area.SquareMeters *
            _referenceData.GetWindowSolarLoadWPerM2(window.Orientation) *
            window.Shgc.Value);
        var wallGain = 0.0;
        foreach (var wall in room.Walls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            wallGain += GetWallLoad(wall);
        }

        var peopleGain = room.PeopleCount * _referenceData.GetPeopleHeatGainW(room.Type);
        var equipmentGain = room.EquipmentLoad.Watts;
        var lightingGain = room.LightingLoad.Watts;
        var totalLoad = baseLoad + windowGain + wallGain + peopleGain + equipmentGain + lightingGain;

        var result = CoolingLoadResultFactory.Create(
            room,
            Method,
            peakHour: 15,
            hourlyHeatLoadW: Enumerable.Repeat(Round(totalLoad), 24).ToList(),
            baseLoad,
            windowGain,
            wallGain,
            peopleGain,
            equipmentGain,
            lightingGain,
            totalLoad,
            deltaT,
            heightAdjustmentFactor: room.HeightM / 3.0,
            temperatureAdjustmentFactor: 1.0,
            reserveFactor,
            cancellationToken);
        return Task.FromResult(result);
    }

    private double GetWallLoad(Wall wall)
    {
        var loadPerM2 = wall.IsExternal
            ? wall.Orientation == CardinalDirection.North
                ? _options.SimplifiedNorthExternalWallLoadWPerM2
                : _options.SimplifiedExternalWallLoadWPerM2
            : _options.SimplifiedInternalWallLoadWPerM2;

        return wall.Area.SquareMeters * loadPerM2;
    }

    private double GetReserveFactor(CalculationPreferences? preferences) =>
        preferences?.CoolingSafetyFactor ?? _options.DefaultCoolingSafetyFactor;

    private static double GetRoomTypeLoadFactor(RoomType roomType) =>
        roomType switch
        {
            RoomType.ServerRoom => 1.4,
            RoomType.MeetingRoom => 1.15,
            RoomType.Retail => 1.1,
            RoomType.Corridor => 0.7,
            RoomType.Residential => 0.9,
            _ => 1.0
        };

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
