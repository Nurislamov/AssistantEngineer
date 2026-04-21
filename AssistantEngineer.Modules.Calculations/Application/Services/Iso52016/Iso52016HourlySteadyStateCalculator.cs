using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016HourlySteadyStateCalculator
{
    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private readonly IAnnualClimateDataProvider _climateDataProvider;
    private readonly ISolarRadiationService _solarRadiationService;
    private readonly IVentilationHeatTransferCalculator? _ventilationCalculator;
    private readonly IWindowShadingService? _windowShadingService;
    private readonly Iso52016EnergyNeedOptions _options;
    private readonly ILogger<Iso52016HourlySteadyStateCalculator> _logger;

    public Iso52016HourlySteadyStateCalculator(
        IAnnualClimateDataProvider climateDataProvider,
        ISolarRadiationService solarRadiationService,
        IVentilationHeatTransferCalculator? ventilationCalculator = null,
        IWindowShadingService? windowShadingService = null,
        Iso52016EnergyNeedOptions? options = null,
        ILogger<Iso52016HourlySteadyStateCalculator>? logger = null)
    {
        _climateDataProvider = climateDataProvider;
        _solarRadiationService = solarRadiationService;
        _ventilationCalculator = ventilationCalculator;
        _windowShadingService = windowShadingService;
        _options = options ?? new Iso52016EnergyNeedOptions();
        _logger = logger ?? NullLogger<Iso52016HourlySteadyStateCalculator>.Instance;
    }

    public async Task<Iso52016AnnualEnergyNeedResult?> CalculateBuildingEnergyNeedsAsync(
        Building building,
        CalculationPreferences? preferences = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var climateZone = building.ClimateZone;
        if (climateZone is null)
            throw new InvalidOperationException("Building must have a climate zone assigned.");

        var weatherYear = year ?? _options.DefaultWeatherYear;
        var annualData = await _climateDataProvider.GetForClimateZoneAsync(
            climateZone.Id,
            weatherYear,
            cancellationToken);
        if (!HasCompleteAnnualWeatherData(annualData))
        {
            _logger.LogWarning(
                "No complete annual climate data found for climate zone {ClimateZoneId} and year {Year}.",
                climateZone.Id,
                weatherYear);
            return null;
        }

        var hourlyData = annualData!.HourlyData
            .Where(hour => hour.HourOfYear.HasValue)
            .OrderBy(hour => hour.HourOfYear!.Value)
            .ToArray();
        var hourlyResults = new List<Iso52016HourlyEnergyNeed>(hourlyData.Length);

        foreach (var zone in GetThermalZoneGroups(building))
        {
            cancellationToken.ThrowIfCancellationRequested();
            hourlyResults.AddRange(CalculateZoneEnergyNeeds(zone, hourlyData, preferences, cancellationToken));
        }

        var groupedHourlyResults = hourlyResults
            .GroupBy(hour => hour.HourOfYear)
            .Select(group => new Iso52016HourlyEnergyNeed(
                group.Key,
                Month: GetMonth(group.Key),
                HeatingLoadW: Round(group.Sum(hour => hour.HeatingLoadW)),
                CoolingLoadW: Round(group.Sum(hour => hour.CoolingLoadW)),
                OperativeTemperatureC: Round(group.Average(hour => hour.OperativeTemperatureC)),
                OutdoorTemperatureC: Round(group.Average(hour => hour.OutdoorTemperatureC)),
                InternalGainsW: Round(group.Sum(hour => hour.InternalGainsW)),
                SolarGainsW: Round(group.Sum(hour => hour.SolarGainsW))))
            .OrderBy(hour => hour.HourOfYear)
            .ToArray();

        var monthlyResults = groupedHourlyResults
            .GroupBy(hour => hour.Month)
            .Select(group => new Iso52016MonthlyEnergyNeed(
                group.Key,
                HeatingDemandKWh: Round(group.Sum(hour => hour.HeatingLoadW) / 1000.0),
                CoolingDemandKWh: Round(group.Sum(hour => hour.CoolingLoadW) / 1000.0)))
            .OrderBy(month => month.Month)
            .ToArray();

        return new Iso52016AnnualEnergyNeedResult(
            building.Id,
            building.Name,
            weatherYear,
            groupedHourlyResults,
            monthlyResults,
            AnnualHeatingDemandKWh: Round(monthlyResults.Sum(month => month.HeatingDemandKWh)),
            AnnualCoolingDemandKWh: Round(monthlyResults.Sum(month => month.CoolingDemandKWh)),
            Breakdown: new Iso52016EnergyBalanceBreakdown(
                SolarGainsKWh: Round(groupedHourlyResults.Sum(hour => hour.SolarGainsW) / 1000.0),
                InternalGainsKWh: Round(groupedHourlyResults.Sum(hour => hour.InternalGainsW) / 1000.0),
                HeatingInputKWh: Round(monthlyResults.Sum(month => month.HeatingDemandKWh)),
                CoolingExtractedKWh: Round(monthlyResults.Sum(month => month.CoolingDemandKWh))));
    }

    public async Task<List<double>> CalculateHourlyCoolingLoadsAsync(
        ThermalZone thermalZone,
        int year = 2020,
        double coolingSetpoint = 26.0,
        CancellationToken cancellationToken = default)
    {
        var climateZone = thermalZone.Building.ClimateZone
            ?? throw new InvalidOperationException("Thermal zone must have a climate zone assigned.");
        var annualData = await _climateDataProvider.GetForClimateZoneAsync(
            climateZone.Id,
            year,
            cancellationToken);
        if (!HasCompleteAnnualWeatherData(annualData))
            return Enumerable.Repeat(0.0, 8760).ToList();

        var hourlyData = annualData!.HourlyData
            .Where(hour => hour.HourOfYear.HasValue)
            .OrderBy(hour => hour.HourOfYear!.Value)
            .ToArray();
        var zone = new ThermalZoneGroup(thermalZone.Name, thermalZone.GetRooms());

        return CalculateZoneEnergyNeeds(
                zone,
                hourlyData,
                preferences: null,
                cancellationToken: cancellationToken,
                coolingSetpointOverride: coolingSetpoint)
            .Select(hour => hour.CoolingLoadW)
            .ToList();
    }

    private IReadOnlyList<Iso52016HourlyEnergyNeed> CalculateZoneEnergyNeeds(
        ThermalZoneGroup zone,
        IReadOnlyList<HourlyClimateData> hourlyData,
        CalculationPreferences? preferences,
        CancellationToken cancellationToken,
        double? coolingSetpointOverride = null)
    {
        var rooms = zone.Rooms;
        var state = CreateThermalZoneState(zone, coolingSetpointOverride, preferences);
        var previousOperativeTemperature = state.HeatingSetpointC;
        var result = new List<Iso52016HourlyEnergyNeed>(hourlyData.Count);

        foreach (var weather in hourlyData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hourOfYear = weather.HourOfYear!.Value;
            var hourOfDay = hourOfYear % 24;
            var dayOfYear = hourOfYear / 24 + 1;
            var heatingSetpoint = GetWeightedScheduleFactor(rooms, room => room.OccupancySchedule, hourOfDay) > 0
                ? state.HeatingSetpointC
                : _options.DefaultHeatingSetbackC;
            var coolingSetpoint = GetWeightedScheduleFactor(rooms, room => room.OccupancySchedule, hourOfDay) > 0
                ? state.CoolingSetpointC
                : _options.DefaultCoolingSetbackC;
            var internalGains = rooms.Sum(room => GetHourlyInternalGain(room, hourOfDay));
            var solarGains = rooms.Sum(room => GetHourlySolarGain(room, weather, dayOfYear, hourOfDay, preferences));
            var ventilationHeatTransfer = GetHourlyVentilationHeatTransfer(
                rooms,
                state,
                weather.DryBulbTemperature,
                weather.WindSpeedMPerS ?? 0,
                hourOfDay);
            var totalHeatTransfer = state.TransmissionHeatTransferCoefficientWPerK +
                ventilationHeatTransfer;
            var thermalCapacityPerHour = state.ThermalCapacityJPerK / 3600.0;
            var denominator = thermalCapacityPerHour + totalHeatTransfer;
            var baseBalance = thermalCapacityPerHour * previousOperativeTemperature +
                totalHeatTransfer * weather.DryBulbTemperature +
                internalGains +
                solarGains;
            var freeFloatingTemperature = denominator > 0
                ? baseBalance / denominator
                : previousOperativeTemperature;
            var heatingLoad = 0.0;
            var coolingLoad = 0.0;
            var operativeTemperature = freeFloatingTemperature;

            if (freeFloatingTemperature < heatingSetpoint)
            {
                heatingLoad = Math.Max(0, heatingSetpoint * denominator - baseBalance);
                heatingLoad *= preferences?.HeatingSafetyFactor ?? 1.0;
                operativeTemperature = heatingSetpoint;
            }
            else if (freeFloatingTemperature > coolingSetpoint)
            {
                coolingLoad = Math.Max(0, baseBalance - coolingSetpoint * denominator);
                coolingLoad *= preferences?.CoolingSafetyFactor ?? 1.0;
                operativeTemperature = coolingSetpoint;
            }

            previousOperativeTemperature = operativeTemperature;
            result.Add(new Iso52016HourlyEnergyNeed(
                hourOfYear,
                Month: GetMonth(hourOfYear),
                HeatingLoadW: Round(heatingLoad),
                CoolingLoadW: Round(coolingLoad),
                OperativeTemperatureC: Round(operativeTemperature),
                OutdoorTemperatureC: Round(weather.DryBulbTemperature),
                InternalGainsW: Round(internalGains),
                SolarGainsW: Round(solarGains)));
        }

        return result;
    }

    private ThermalZoneState CreateThermalZoneState(
        ThermalZoneGroup zone,
        double? coolingSetpointOverride,
        CalculationPreferences? preferences)
    {
        var rooms = zone.Rooms;
        var floorArea = rooms.Sum(room => room.Area.SquareMeters);
        var volume = rooms.Sum(room => room.CalculateVolume());
        var transmission = rooms.Sum(GetTransmissionHeatTransferCoefficient);
        var ventilation = rooms.Sum(room =>
        {
            var airChangesPerHour = room.VentilationParameters?.AirChangesPerHour ??
                GetDefaultAirChangesPerHour(preferences);
            var heatRecoveryFactor = 1 - (room.VentilationParameters?.HeatRecoveryEfficiency ?? 0);
            return _options.AirHeatCapacityWhPerM3K *
                airChangesPerHour *
                room.CalculateVolume() *
                heatRecoveryFactor;
        });
        var envelopeCapacity = rooms.Sum(room => room.CalculateInternalHeatCapacityKjPerK() * 1000.0);
        var thermalCapacity = Math.Max(
            envelopeCapacity + floorArea * GetInternalHeatCapacityJPerM2K(preferences),
            Math.Max(floorArea, 1.0) * GetInternalHeatCapacityJPerM2K(preferences));
        var heatingSetpoint = WeightedAverage(rooms, room => room.IndoorTemperature.Celsius);
        var coolingSetpoint = coolingSetpointOverride ?? Math.Max(_options.DefaultCoolingSetpointC, heatingSetpoint);

        return new ThermalZoneState(
            FloorAreaM2: floorArea,
            VolumeM3: volume,
            TransmissionHeatTransferCoefficientWPerK: transmission,
            VentilationHeatTransferCoefficientWPerK: ventilation,
            ThermalCapacityJPerK: thermalCapacity,
            HeatingSetpointC: heatingSetpoint,
            CoolingSetpointC: coolingSetpoint);
    }

    private double GetHourlySolarGain(
        Room room,
        HourlyClimateData weather,
        int dayOfYear,
        int hourOfDay,
        CalculationPreferences? preferences)
    {
        return room.Windows.Sum(window =>
        {
            var radiation = _solarRadiationService.CalculateVerticalSurfaceRadiation(
                weather,
                window.Orientation,
                _options.LatitudeDegrees,
                dayOfYear,
                hourOfDay);
            var shadingReduction = GetWindowShadingReduction(window, dayOfYear, hourOfDay, preferences);
            return window.Area.SquareMeters *
                window.Shgc.Value *
                radiation *
                (1 - GetWindowFrameAreaFraction(preferences)) *
                shadingReduction *
                GetSolarUtilizationFactor(preferences);
        });
    }

    private double GetWindowShadingReduction(
        Window window,
        int dayOfYear,
        int hourOfDay,
        CalculationPreferences? preferences)
    {
        var configuredReduction = Math.Clamp(GetDirectSolarShadingReductionFactor(preferences), 0, 1);
        if (_windowShadingService is null)
            return configuredReduction;

        var shading = window.Shading;
        var hasWindowSpecificShading =
            shading.OverhangDepthM > 0 ||
            shading.SideFinDepthM > 0 ||
            shading.RevealDepthM > 0 ||
            shading.WindowHeightM > 0 ||
            shading.WindowWidthM > 0 ||
            Math.Abs(shading.MinimumDirectSolarReductionFactor - WindowShadingParameters.None.MinimumDirectSolarReductionFactor) > 0.0001 ||
            Math.Abs(shading.DiffuseSolarShareUnaffected - WindowShadingParameters.None.DiffuseSolarShareUnaffected) > 0.0001;
        var windowHeight = shading.WindowHeightM > 0
            ? shading.WindowHeightM
            : _options.DefaultWindowHeightM;
        var windowWidth = shading.WindowWidthM > 0
            ? shading.WindowWidthM
            : _options.DefaultWindowWidthM;
        var geometricReduction = _windowShadingService.CalculateCombinedSolarReduction(
            window.Orientation,
            _options.LatitudeDegrees,
            dayOfYear,
            hourOfDay,
            new WindowShadingOptions(
                shading.OverhangDepthM > 0 ? shading.OverhangDepthM : _options.DefaultOverhangDepthM,
                shading.SideFinDepthM > 0 ? shading.SideFinDepthM : _options.DefaultSideFinDepthM,
                shading.RevealDepthM > 0 ? shading.RevealDepthM : _options.DefaultWindowRevealDepthM,
                windowHeight,
                windowWidth,
                hasWindowSpecificShading
                    ? shading.MinimumDirectSolarReductionFactor
                    : _options.MinimumDirectSolarShadingReductionFactor,
                hasWindowSpecificShading
                    ? shading.DiffuseSolarShareUnaffected
                    : GetDiffuseSolarShareUnaffectedByShading(preferences)));
        return Math.Clamp(configuredReduction * geometricReduction, 0, 1);
    }

    private double GetInternalHeatCapacityJPerM2K(CalculationPreferences? preferences) =>
        preferences?.Iso52016InternalHeatCapacityJPerM2K > 0
            ? preferences.Iso52016InternalHeatCapacityJPerM2K
            : _options.InternalHeatCapacityJPerM2K;

    private double GetSolarUtilizationFactor(CalculationPreferences? preferences) =>
        preferences is null
            ? _options.DefaultSolarUtilizationFactor
            : Math.Clamp(preferences.Iso52016SolarUtilizationFactor, 0, 1);

    private double GetWindowFrameAreaFraction(CalculationPreferences? preferences) =>
        preferences is null
            ? _options.DefaultWindowFrameAreaFraction
            : Math.Clamp(preferences.Iso52016WindowFrameAreaFraction, 0, 0.9);

    private double GetDirectSolarShadingReductionFactor(CalculationPreferences? preferences) =>
        preferences is null
            ? _options.DefaultDirectSolarShadingReductionFactor
            : Math.Clamp(preferences.Iso52016DirectSolarShadingReductionFactor, 0, 1);

    private double GetDiffuseSolarShareUnaffectedByShading(CalculationPreferences? preferences) =>
        preferences is null
            ? _options.DiffuseSolarShareUnaffectedByShading
            : Math.Clamp(preferences.Iso52016DiffuseSolarShareUnaffectedByShading, 0, 1);

    private double GetDefaultAirChangesPerHour(CalculationPreferences? preferences) =>
        preferences?.Iso52016DefaultAirChangesPerHour >= 0
            ? preferences.Iso52016DefaultAirChangesPerHour
            : _options.DefaultAirChangesPerHour;

    private double GetHourlyVentilationHeatTransfer(
        IReadOnlyCollection<Room> rooms,
        ThermalZoneState state,
        double outdoorTemperatureC,
        double windSpeedMPerS,
        int hourOfDay)
    {
        var scheduleFactor = GetWeightedScheduleFactor(rooms, room => room.OccupancySchedule, hourOfDay);
        if (_ventilationCalculator is null)
            return state.VentilationHeatTransferCoefficientWPerK * scheduleFactor;

        var infiltration = rooms.Sum(room => _ventilationCalculator.CalculateInfiltration(
            room,
            new VentilationCalculationContext(
                VentilationCalculationMethod.TemperatureWind,
                state.HeatingSetpointC,
                outdoorTemperatureC,
                windSpeedMPerS)));

        if (scheduleFactor <= 0)
        {
            return infiltration + rooms.Sum(room => _ventilationCalculator.CalculateMechanical(
                room,
                new VentilationCalculationContext(
                    VentilationCalculationMethod.FixedAirChanges,
                    state.HeatingSetpointC,
                    outdoorTemperatureC)));
        }

        return infiltration + rooms.Sum(room => _ventilationCalculator.CalculateMechanical(
            room,
            new VentilationCalculationContext(
                VentilationCalculationMethod.Occupancy,
                state.HeatingSetpointC,
                outdoorTemperatureC))) * scheduleFactor;
    }

    private static double GetHourlyInternalGain(Room room, int hour)
    {
        var people = room.PeopleCount * GetPeopleHeatGain(room.Type) *
            GetScheduleFactor(room.OccupancySchedule, hour);
        var equipment = room.EquipmentLoad.Watts *
            GetScheduleFactor(room.EquipmentSchedule, hour);
        var lighting = room.LightingLoad.Watts *
            GetScheduleFactor(room.LightingSchedule, hour);
        return people + equipment + lighting;
    }

    private static double GetTransmissionHeatTransferCoefficient(Room room)
    {
        var envelope = room.Walls
            .Where(wall => wall.IsExternal)
            .Sum(wall => wall.Area.SquareMeters * GetWallUValue(wall));
        envelope += room.Windows.Sum(window => window.Area.SquareMeters * window.UValue.Value);
        envelope += room.Area.SquareMeters * room.GetFloorUValue();
        envelope += room.Area.SquareMeters * room.GetCeilingUValue();
        return envelope;
    }

    private static IReadOnlyList<ThermalZoneGroup> GetThermalZoneGroups(Building building)
    {
        var allRooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToArray();
        if (building.ThermalZones.Count == 0)
            return [new ThermalZoneGroup("Building", allRooms)];

        var roomsById = allRooms
            .Where(room => room.Id > 0)
            .ToDictionary(room => room.Id);
        var countedRoomIds = new HashSet<int>();
        var groups = new List<ThermalZoneGroup>();

        foreach (var zone in building.ThermalZones.OrderBy(zone => zone.Id))
        {
            var zoneRooms = zone.RoomIds
                .Where(countedRoomIds.Add)
                .Select(roomId => roomsById.TryGetValue(roomId, out var room) ? room : null)
                .Where(room => room is not null)
                .Select(room => room!)
                .ToArray();
            if (zoneRooms.Length > 0)
                groups.Add(new ThermalZoneGroup(zone.Name, zoneRooms));
        }

        var unassignedRooms = allRooms
            .Where(room => room.Id <= 0 || !countedRoomIds.Contains(room.Id))
            .ToArray();
        if (unassignedRooms.Length > 0)
            groups.Add(new ThermalZoneGroup("Unassigned rooms", unassignedRooms));

        return groups;
    }

    private static bool HasCompleteAnnualWeatherData(AnnualClimateData? annualData) =>
        annualData is not null &&
        annualData.HourlyData
            .Select(hour => hour.HourOfYear)
            .Where(hour => hour.HasValue)
            .Select(hour => hour!.Value)
            .Distinct()
            .OrderBy(hour => hour)
            .SequenceEqual(Enumerable.Range(0, 8760));

    private static double GetWeightedScheduleFactor(
        IReadOnlyCollection<Room> rooms,
        Func<Room, HourlySchedule?> scheduleSelector,
        int hour) =>
        WeightedAverage(rooms, room => GetScheduleFactor(scheduleSelector(room), hour));

    private static double WeightedAverage(IReadOnlyCollection<Room> rooms, Func<Room, double> valueSelector)
    {
        var totalArea = rooms.Sum(room => room.Area.SquareMeters);
        if (totalArea <= 0)
            return rooms.Count == 0 ? 0 : rooms.Average(valueSelector);

        return rooms.Sum(room => valueSelector(room) * room.Area.SquareMeters) / totalArea;
    }

    private static double GetScheduleFactor(HourlySchedule? schedule, int hour) =>
        schedule?.Factors.Count == 24
            ? schedule.Factors[hour]
            : 1.0;

    private static double GetPeopleHeatGain(RoomType type) => type switch
    {
        RoomType.Office => 125,
        RoomType.MeetingRoom => 125,
        RoomType.Corridor => 80,
        RoomType.ServerRoom => 125,
        RoomType.Retail => 170,
        RoomType.Residential => 80,
        _ => 125
    };

    private static double GetWallUValue(Wall wall) =>
        wall.ConstructionAssembly is { UValueWPerM2K: > 0 } assembly
            ? assembly.UValueWPerM2K
            : wall.UValue.Value;

    private static int GetMonth(int hourOfYear)
    {
        var dayOfYear = hourOfYear / 24;
        var accumulatedDays = 0;
        for (var month = 1; month <= DaysPerMonth.Length; month++)
        {
            accumulatedDays += DaysPerMonth[month - 1];
            if (dayOfYear < accumulatedDays)
                return month;
        }

        return 12;
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record ThermalZoneGroup(string Name, IReadOnlyCollection<Room> Rooms);

    private sealed record ThermalZoneState(
        double FloorAreaM2,
        double VolumeM3,
        double TransmissionHeatTransferCoefficientWPerK,
        double VentilationHeatTransferCoefficientWPerK,
        double ThermalCapacityJPerK,
        double HeatingSetpointC,
        double CoolingSetpointC);
}