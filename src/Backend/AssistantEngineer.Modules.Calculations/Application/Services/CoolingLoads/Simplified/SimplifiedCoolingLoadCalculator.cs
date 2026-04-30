using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Services.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Simplified;

public sealed class SimplifiedCoolingLoadCalculator : IRoomCoolingLoadCalculationStrategy
{
    private readonly CoolingLoadCalculationOptions _options;
    private readonly ICoolingLoadReferenceData _referenceData;
    private readonly WindowSolarGainEngine _windowSolarGains;
    private readonly TransmissionHeatTransferEngine _transmissionHeatTransfer;
    private readonly InternalGainEngine _internalGains;

    public SimplifiedCoolingLoadCalculator(
        IOptions<CoolingLoadCalculationOptions> options,
        ICoolingLoadReferenceData referenceData,
        WindowSolarGainEngine? windowSolarGains = null,
        TransmissionHeatTransferEngine? transmissionHeatTransfer = null,
        InternalGainEngine? internalGains = null)
    {
        _options = options.Value;
        _referenceData = referenceData;
        _windowSolarGains = windowSolarGains ?? new WindowSolarGainEngine();
        _transmissionHeatTransfer = transmissionHeatTransfer ?? new TransmissionHeatTransferEngine();
        _internalGains = internalGains ?? new InternalGainEngine();
    }

    public CoolingLoadCalculationMethod Method => CoolingLoadCalculationMethod.Simplified;

    public Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reserveFactor = GetReserveFactor(preferences);
        var outdoorTemperature = GetOutdoorDesignTemperature(room);
        var deltaT = Math.Abs(outdoorTemperature - room.IndoorTemperature.Celsius);
        var baseLoad = room.CalculateVolume() * _options.SimplifiedVolumeLoadWPerM3 * GetRoomTypeLoadFactor(room.Type);
        var windowGain = CalculateWindowSolarGain(room);
        var transmissionGain = CalculateTransmissionCoolingContribution(
            room,
            outdoorTemperature);

        var internalGains = _internalGains.Calculate(
            new InternalGainInput(
                RoomId: room.Id,
                AreaM2: room.Area.SquareMeters,
                OccupancyPeople: room.PeopleCount,
                SensibleGainPerPersonW: _referenceData.GetPeopleHeatGainW(room.Type),
                EquipmentLoadW: room.EquipmentLoad.Watts,
                LightingLoadW: room.LightingLoad.Watts,
                DiagnosticsContext: $"Room {room.Id} simplified cooling internal gains"));
        var peopleGain = internalGains.Value.OccupancySensibleGainW;
        var equipmentGain = internalGains.Value.EquipmentGainW;
        var lightingGain = internalGains.Value.LightingGainW;
        var totalLoad = Math.Max(
            0,
            baseLoad + windowGain + transmissionGain + peopleGain + equipmentGain + lightingGain);

        var result = CoolingLoadResultFactory.Create(
            room,
            Method,
            peakHour: 15,
            hourlyHeatLoadW: Enumerable.Repeat(Round(totalLoad), 24).ToList(),
            baseLoad,
            windowGain,
            transmissionGain,
            ventilationGain: 0,
            infiltrationGain: 0,
            naturalVentilationGain: 0,
            peopleGain,
            equipmentGain,
            lightingGain,
            totalLoad,
            deltaT,
            outdoorTemperature,
            heightAdjustmentFactor: room.HeightM / 3.0,
            temperatureAdjustmentFactor: 1.0,
            reserveFactor,
            cancellationToken);
        return Task.FromResult(result);
    }

    private double CalculateTransmissionCoolingContribution(
        Room room,
        double outdoorTemperatureC)
    {
        var transmission = _transmissionHeatTransfer.Calculate(
            RoomTransmissionInputFactory.CreateForRoom(
                room,
                room.IndoorTemperature.Celsius,
                outdoorTemperatureC));

        return transmission.Value.TotalHeatGainW - transmission.Value.TotalHeatLossW;
    }

    private double CalculateWindowSolarGain(Room room) =>
        room.Windows.Sum(window =>
        {
            var solar = _windowSolarGains.Calculate(
                WindowSolarGainInputFactory.CreateForWindow(
                    window,
                    _referenceData.GetWindowSolarLoadWPerM2(window.Orientation)));

            return solar.Value.SolarGainW;
        });

    private double GetReserveFactor(CalculationPreferences? preferences) =>
        preferences?.CoolingSafetyFactor ?? _options.DefaultCoolingSafetyFactor;

    private double GetOutdoorDesignTemperature(Room room) =>
        room.OutdoorTemperatureOverride?.Celsius ??
        room.Floor.Building.ClimateZone?.SummerDesignTemperature.Celsius ??
        _options.DefaultOutdoorCoolingDesignTemperatureC;

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
