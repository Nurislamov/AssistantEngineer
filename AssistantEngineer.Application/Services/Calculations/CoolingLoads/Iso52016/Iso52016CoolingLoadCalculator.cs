using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Models.Construction;
using AssistantEngineer.Domain.Models.Schedules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Calculations.CoolingLoads.Iso52016;

public sealed class Iso52016CoolingLoadOptions
{
    public int DefaultDesignMonth { get; init; } = 7;
    public double DefaultThermalMassWhPerM2K { get; init; } = 45.0;
    public double DefaultVentilationAirChangesPerHour { get; init; } = 0.5;
    public double AirHeatCapacityWhPerM3K { get; init; } = 0.34;
    public double DefaultSolarUtilizationFactor { get; init; } = 0.75;
    public double DefaultCoolingSafetyFactor { get; init; } = 1.10;
}

public sealed class Iso52016CoolingLoadCalculator : IRoomCoolingLoadCalculationStrategy
{
    private readonly Iso52016CoolingLoadOptions _options;
    private readonly IIso52016ReferenceDataProvider _referenceDataProvider;
    private readonly IHourlyProfileAggregator _profileAggregator;
    private readonly ILogger<Iso52016CoolingLoadCalculator> _logger;

    public Iso52016CoolingLoadCalculator(
        Iso52016CoolingLoadOptions options,
        IIso52016ReferenceDataProvider referenceDataProvider,
        IHourlyProfileAggregator profileAggregator,
        ILogger<Iso52016CoolingLoadCalculator>? logger = null)
    {
        _options = options;
        _referenceDataProvider = referenceDataProvider;
        _profileAggregator = profileAggregator;
        _logger = logger ?? NullLogger<Iso52016CoolingLoadCalculator>.Instance;
    }

    public CoolingLoadCalculationMethod Method => CoolingLoadCalculationMethod.Iso52016;

    public async Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ISO 52016 cooling calculation started for room {RoomId}.", room.Id);

        var climateZone = room.Floor.Building.ClimateZone;
        var climateOutdoorTemperatureProfile = climateZone is not null
            ? await _referenceDataProvider.GetOutdoorTemperatureProfileAsync(
                climateZone,
                _options.DefaultDesignMonth,
                cancellationToken)
            : null;
        var outdoorTemperatureProfile = climateOutdoorTemperatureProfile is { Count: 24 }
            ? climateOutdoorTemperatureProfile
            : null;
        var designDeltaT = outdoorTemperatureProfile is not null
            ? outdoorTemperatureProfile.Max(outdoorTemperature => Math.Max(outdoorTemperature - room.IndoorTemperature.Celsius, 0))
            : Math.Max(room.OutdoorTemperature.Celsius - room.IndoorTemperature.Celsius, 0);
        outdoorTemperatureProfile ??= CreateOutdoorTemperatureProfile(room.IndoorTemperature.Celsius, designDeltaT);
        var reserveFactor = preferences?.CoolingSafetyFactor ?? _options.DefaultCoolingSafetyFactor;
        var solarRadiationProfiles = climateZone is not null
            ? await _referenceDataProvider.GetSolarRadiationAsync(climateZone, _options.DefaultDesignMonth, cancellationToken)
            : new Dictionary<CardinalDirection, IReadOnlyList<double>>();

        var transmissionProfile = CreateTransmissionProfile(room, outdoorTemperatureProfile, cancellationToken);
        var ventilationProfile = CreateVentilationProfile(room, outdoorTemperatureProfile, cancellationToken);
        var solarProfile = CreateSolarProfile(room, solarRadiationProfiles, cancellationToken);
        var internalGainProfile = CreateInternalGainProfile(room, cancellationToken);

        var rawLoadProfile = _profileAggregator.SumProfiles(
            [transmissionProfile, ventilationProfile, solarProfile, internalGainProfile],
            cancellationToken);
        var hourlyHeatLoad = ApplyThermalMassDamping(room, rawLoadProfile, cancellationToken);
        var peakHour = _profileAggregator.FindPeakHour(hourlyHeatLoad);
        var totalLoad = hourlyHeatLoad[peakHour];
        var peopleGain = GetPeopleGain(room, peakHour);
        var equipmentGain = GetScheduledGain(room.EquipmentLoad.Watts, room.EquipmentSchedule, peakHour);
        var lightingGain = GetScheduledGain(room.LightingLoad.Watts, room.LightingSchedule, peakHour);

        var result = CoolingLoadResultFactory.Create(
            room,
            Method,
            peakHour,
            hourlyHeatLoad,
            baseLoad: transmissionProfile[peakHour] + ventilationProfile[peakHour],
            windowGain: solarProfile[peakHour],
            wallGain: transmissionProfile[peakHour],
            peopleGain,
            equipmentGain,
            lightingGain,
            totalLoad,
            deltaT: designDeltaT,
            heightAdjustmentFactor: room.HeightM / 3.0,
            temperatureAdjustmentFactor: outdoorTemperatureProfile[peakHour] - room.IndoorTemperature.Celsius,
            reserveFactor,
            cancellationToken);
        _logger.LogDebug(
            "ISO 52016 cooling calculation finished for room {RoomId}: peak hour {PeakHour}, total load {TotalHeatLoadW} W.",
            room.Id,
            result.PeakHour,
            result.TotalHeatLoadW);
        return result;
    }

    private static IReadOnlyList<double> CreateOutdoorTemperatureProfile(double indoorTemperatureC, double designDeltaT)
    {
        var factors = new[] { 0.58, 0.55, 0.53, 0.52, 0.52, 0.55, 0.62, 0.72, 0.82, 0.90, 0.96, 1.00, 0.98, 0.95, 0.92, 0.90, 0.86, 0.80, 0.74, 0.70, 0.66, 0.63, 0.61, 0.60 };
        return factors.Select(factor => indoorTemperatureC + designDeltaT * factor).ToArray();
    }

    private static List<double> CreateTransmissionProfile(
        Room room,
        IReadOnlyList<double> outdoorTemperatureProfile,
        CancellationToken cancellationToken)
    {
        var result = new List<double>(capacity: 24);
        for (var hour = 0; hour < 24; hour++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deltaT = Math.Max(outdoorTemperatureProfile[hour] - room.IndoorTemperature.Celsius, 0);
            var load = room.Walls
                .Where(wall => wall.IsExternal)
                .Sum(wall => wall.Area.SquareMeters * GetWallUValue(wall) * deltaT);
            load += room.Windows.Sum(window => window.Area.SquareMeters * window.UValue.Value * deltaT);
            result.Add(Math.Round(load, 2, MidpointRounding.AwayFromZero));
        }
        return result;
    }

    private List<double> CreateVentilationProfile(
        Room room,
        IReadOnlyList<double> outdoorTemperatureProfile,
        CancellationToken cancellationToken)
    {
        var result = new List<double>(capacity: 24);
        var airChangesPerHour = room.VentilationParameters?.AirChangesPerHour ??
            _options.DefaultVentilationAirChangesPerHour;
        var heatRecoveryFactor = 1 - (room.VentilationParameters?.HeatRecoveryEfficiency ?? 0);

        for (var hour = 0; hour < 24; hour++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deltaT = Math.Max(outdoorTemperatureProfile[hour] - room.IndoorTemperature.Celsius, 0);
            var infiltrationAirChangesPerHour = room.VentilationParameters is null
                ? 0
                : room.VentilationParameters.InfiltrationAirChangesPerHour +
                room.VentilationParameters.StackCoefficient * Math.Sqrt(deltaT);
            result.Add(Math.Round(
                (_options.AirHeatCapacityWhPerM3K *
                airChangesPerHour *
                room.CalculateVolume() *
                deltaT *
                heatRecoveryFactor) +
                (_options.AirHeatCapacityWhPerM3K *
                infiltrationAirChangesPerHour *
                room.CalculateVolume() *
                deltaT),
                2,
                MidpointRounding.AwayFromZero));
        }
        return result;
    }

    private List<double> CreateSolarProfile(
        Room room,
        IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>> solarRadiationProfiles,
        CancellationToken cancellationToken)
    {
        var result = new List<double>(capacity: 24);
        var daylightProfile = CreateDaylightProfile();
        for (var hour = 0; hour < 24; hour++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var load = room.Windows.Sum(window =>
            {
                var radiation = solarRadiationProfiles.TryGetValue(window.Orientation, out var profile)
                    ? profile[hour]
                    : _referenceDataProvider.GetDefaultSolarRadiation(window.Orientation) * daylightProfile[hour];
                return window.Area.SquareMeters *
                    window.Shgc.Value *
                    radiation *
                    _options.DefaultSolarUtilizationFactor;
            });
            result.Add(Math.Round(load, 2, MidpointRounding.AwayFromZero));
        }
        return result;
    }

    private List<double> CreateInternalGainProfile(Room room, CancellationToken cancellationToken)
    {
        var defaultProfile = CreateDefaultOccupancyProfile();
        var result = new List<double>(capacity: 24);

        for (var hour = 0; hour < 24; hour++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var peopleGain = room.PeopleCount *
                _referenceDataProvider.GetPeopleHeatGain(room.Type) *
                GetScheduleFactor(room.OccupancySchedule, defaultProfile, hour);
            var equipmentGain = room.EquipmentLoad.Watts *
                GetScheduleFactor(room.EquipmentSchedule, defaultProfile, hour);
            var lightingGain = room.LightingLoad.Watts *
                GetScheduleFactor(room.LightingSchedule, defaultProfile, hour);
            result.Add(Math.Round(peopleGain + equipmentGain + lightingGain, 2, MidpointRounding.AwayFromZero));
        }
        return result;
    }

    private List<double> ApplyThermalMassDamping(
        Room room,
        IReadOnlyList<double> rawLoadProfile,
        CancellationToken cancellationToken)
    {
        var thermalMassWhPerM2K = GetThermalMassWhPerM2K(room);
        var dampingFactor = Math.Clamp(
            thermalMassWhPerM2K / (room.Area.SquareMeters * 10.0),
            0.15,
            0.65);
        var result = new List<double>(capacity: 24);

        for (var hour = 0; hour < 24; hour++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var previousHour = (hour + 23) % 24;
            var dampedLoad = rawLoadProfile[hour] * (1 - dampingFactor) +
                rawLoadProfile[previousHour] * dampingFactor;
            result.Add(Math.Round(dampedLoad, 2, MidpointRounding.AwayFromZero));
        }
        return result;
    }

    private static IReadOnlyList<double> CreateDaylightProfile() =>
        [0, 0, 0, 0, 0, 0.05, 0.2, 0.45, 0.7, 0.88, 1.0, 0.95, 0.9, 0.82, 0.65, 0.42, 0.18, 0.04, 0, 0, 0, 0, 0, 0];

    private static IReadOnlyList<double> CreateDefaultOccupancyProfile() =>
        [0, 0, 0, 0, 0, 0.05, 0.2, 0.65, 1.0, 1.0, 1.0, 0.95, 0.85, 0.95, 1.0, 1.0, 0.85, 0.35, 0.1, 0.05, 0, 0, 0, 0];

    private static double GetWallUValue(Wall wall) =>
        wall.ConstructionAssembly is { UValueWPerM2K: > 0 } assembly
            ? assembly.UValueWPerM2K
            : wall.UValue.Value;

    private double GetPeopleGain(Room room, int hour) =>
        room.PeopleCount *
        _referenceDataProvider.GetPeopleHeatGain(room.Type) *
        GetScheduleFactor(room.OccupancySchedule, CreateDefaultOccupancyProfile(), hour);

    private static double GetScheduledGain(double gain, HourlySchedule? schedule, int hour) =>
        gain * GetScheduleFactor(schedule, CreateDefaultOccupancyProfile(), hour);

    private static double GetScheduleFactor(
        HourlySchedule? schedule,
        IReadOnlyList<double> fallbackProfile,
        int hour) =>
        schedule?.Factors.Count == 24
            ? schedule.Factors[hour]
            : fallbackProfile[hour];

    private double GetThermalMassWhPerM2K(Room room)
    {
        var wallsWithAssemblies = room.Walls
            .Where(wall => wall.ConstructionAssembly is { Layers.Count: > 0 })
            .ToArray();

        var totalArea = wallsWithAssemblies.Sum(wall => wall.Area.SquareMeters);
        if (totalArea <= 0)
            return _options.DefaultThermalMassWhPerM2K;

        var weightedCapacity = wallsWithAssemblies.Sum(wall =>
            wall.Area.SquareMeters *
            GetArealHeatCapacityWhPerM2K(wall.ConstructionAssembly!));

        var capacity = weightedCapacity / totalArea;
        return capacity > 0 ? capacity : _options.DefaultThermalMassWhPerM2K;
    }

    private static double GetArealHeatCapacityWhPerM2K(ConstructionAssembly assembly) =>
        assembly.Layers.Sum(layer =>
            layer.Material.VolumetricHeatCapacityKjPerM3K *
            layer.ThicknessM /
            3.6);
}


