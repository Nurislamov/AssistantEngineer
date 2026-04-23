using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016HourlySteadyStateCalculator
{
    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private readonly IAnnualClimateDataProvider _climateDataProvider;
    private readonly ISolarRadiationService _solarRadiationService;
    private readonly IVentilationHeatTransferCalculator? _ventilationCalculator;
    private readonly IWindowShadingService? _windowShadingService;
    private readonly IBuildingEnvelopeReferenceData _envelopeReferenceData;
    private readonly IEn16798ProfileCatalog _profileCatalog;
    private readonly INaturalVentilationAirflowService? _naturalVentilationAirflowService;
    private readonly Iso52016EnergyNeedOptions _options;
    private readonly En16798ProfileOptions _profileOptions;
    private readonly ILogger<Iso52016HourlySteadyStateCalculator> _logger;

    public Iso52016HourlySteadyStateCalculator(
        IAnnualClimateDataProvider climateDataProvider,
        ISolarRadiationService solarRadiationService,
        IVentilationHeatTransferCalculator? ventilationCalculator = null,
        IWindowShadingService? windowShadingService = null,
        IBuildingEnvelopeReferenceData? envelopeReferenceData = null,
        IEn16798ProfileCatalog? profileCatalog = null,
        INaturalVentilationAirflowService? naturalVentilationAirflowService = null,
        IOptions<Iso52016EnergyNeedOptions>? options = null,
        IOptions<En16798ProfileOptions>? profileOptions = null,
        ILogger<Iso52016HourlySteadyStateCalculator>? logger = null)
    {
        _climateDataProvider = climateDataProvider;
        _solarRadiationService = solarRadiationService;
        _ventilationCalculator = ventilationCalculator;
        _windowShadingService = windowShadingService;
        _envelopeReferenceData = envelopeReferenceData ?? new BuildingEnvelopeReferenceData();
        _profileCatalog = profileCatalog ?? new En16798ProfileCatalog();
        _naturalVentilationAirflowService = naturalVentilationAirflowService;
        _options = options?.Value ?? new Iso52016EnergyNeedOptions();
        _profileOptions = profileOptions?.Value ?? new En16798ProfileOptions();
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

        var zones = GetThermalZoneGroups(building);
        var zoneStates = zones.ToDictionary(
            zone => zone.Name,
            zone => CreateThermalZoneState(zone, preferences));

        var roomZoneMap = zones
            .SelectMany(zone => zone.Rooms.Select(room => new { room.Id, zone.Name }))
            .ToDictionary(x => x.Id, x => x.Name);

        var previousZoneTemperatures = zoneStates.ToDictionary(
            zone => zone.Key,
            zone => zone.Value.HeatingSetpointC);

        var zoneHourlyResults = new List<Iso52016ZoneHourlyEnergyNeed>(hourlyData.Length * Math.Max(zones.Count, 1));
        var roomHourlyResults =
            new List<Iso52016RoomHourlyEnergyNeed>(hourlyData.Length *
                                                   Math.Max(building.Floors.SelectMany(x => x.Rooms).Count(), 1));
        var previousRoomTemperatures = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToDictionary(room => room.Id, room => room.IndoorTemperature.Celsius);

        foreach (var weather in hourlyData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentHourResults = new List<ZoneHourResult>(zones.Count);

            foreach (var zone in zones)
            {
                var zoneState = zoneStates[zone.Name];
                currentHourResults.Add(CalculateZoneHourEnergyNeed(
                    zone,
                    zoneState,
                    weather,
                    previousRoomTemperatures,
                    roomZoneMap,
                    preferences,
                    cancellationToken));
            }

            foreach (var zoneResult in currentHourResults)
            {
                previousZoneTemperatures[zoneResult.ZoneName] = zoneResult.Hour.OperativeTemperatureC;

                foreach (var roomResult in zoneResult.Rooms)
                    previousRoomTemperatures[roomResult.RoomId] = roomResult.Hour.OperativeTemperatureC;
            }

            zoneHourlyResults.AddRange(currentHourResults.Select(result => new Iso52016ZoneHourlyEnergyNeed(
                ZoneName: result.ZoneName,
                HourOfYear: result.Hour.HourOfYear,
                Month: result.Hour.Month,
                HeatingLoadW: result.Hour.HeatingLoadW,
                CoolingLoadW: result.Hour.CoolingLoadW,
                OperativeTemperatureC: result.Hour.OperativeTemperatureC,
                OutdoorTemperatureC: result.Hour.OutdoorTemperatureC,
                InternalGainsW: result.Hour.InternalGainsW,
                SolarGainsW: result.Hour.SolarGainsW)));

            roomHourlyResults.AddRange(currentHourResults
                .SelectMany(result => result.Rooms)
                .Select(result => result.Hour));
        }

        var groupedHourlyResults = zoneHourlyResults
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
                CoolingExtractedKWh: Round(monthlyResults.Sum(month => month.CoolingDemandKWh))),
            ZoneHourlyResults: zoneHourlyResults,
            RoomHourlyResults: roomHourlyResults);
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

        var zone = new ThermalZoneGroup(thermalZone.Name, thermalZone.AssignedRooms);
        var state = CreateThermalZoneState(zone, preferences: null, coolingSetpointOverride: coolingSetpoint);

        var previousRoomTemperatures = zone.Rooms
            .ToDictionary(room => room.Id, room => room.IndoorTemperature.Celsius);

        var roomZoneMap = zone.Rooms.ToDictionary(room => room.Id, _ => zone.Name);
        var result = new List<double>(capacity: hourlyData.Length);

        foreach (var weather in hourlyData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var zoneResult = CalculateZoneHourEnergyNeed(
                zone,
                state,
                weather,
                previousRoomTemperatures,
                roomZoneMap,
                preferences: null,
                cancellationToken);

            foreach (var roomResult in zoneResult.Rooms)
                previousRoomTemperatures[roomResult.RoomId] = roomResult.Hour.OperativeTemperatureC;

            result.Add(zoneResult.Hour.CoolingLoadW);
        }

        return result;
    }

    private ZoneHourResult CalculateZoneHourEnergyNeed(
        ThermalZoneGroup zone,
        ThermalZoneState state,
        HourlyClimateData weather,
        IReadOnlyDictionary<int, double> previousRoomTemperatures,
        IReadOnlyDictionary<int, string> roomZoneMap,
        CalculationPreferences? preferences,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rooms = zone.Rooms;
        var hourOfYear = weather.HourOfYear!.Value;
        var hourOfDay = hourOfYear % 24;
        var dayOfYear = hourOfYear / 24 + 1;

        var occupiedScheduleFactor = WeightedAverage(rooms, room => GetOccupancyFactor(room, hourOfDay));
        var heatingSetpoint = occupiedScheduleFactor > 0
            ? state.HeatingSetpointC
            : _options.DefaultHeatingSetbackC;
        var coolingSetpoint = occupiedScheduleFactor > 0
            ? state.CoolingSetpointC
            : _options.DefaultCoolingSetbackC;

        var roomResults = new List<RoomHourResult>(rooms.Count);

        foreach (var room in rooms)
        {
            var previousRoomTemperature = previousRoomTemperatures.TryGetValue(room.Id, out var previousTemp)
                ? previousTemp
                : room.IndoorTemperature.Celsius;

            var internalGains = GetHourlyInternalGain(room, hourOfDay);
            var solarGains = GetHourlySolarGain(room, weather, dayOfYear, hourOfDay, preferences);
            var ventilationHeatTransfer = GetHourlyVentilationHeatTransferForRoom(
                room,
                heatingSetpoint,
                weather.DryBulbTemperature,
                weather.WindSpeedMPerS ?? 0,
                hourOfDay);

            var adjacent = CalculateAdjacentBoundaryContributionForRoom(
                room,
                heatingSetpoint,
                weather.DryBulbTemperature,
                previousRoomTemperatures,
                roomZoneMap);

            var outdoorUa = GetOutdoorEnvelopeHeatTransferCoefficient(room, _envelopeReferenceData.GetDefaults());
            var groundUa = GetGroundEnvelopeHeatTransferCoefficient(room, _envelopeReferenceData.GetDefaults());

            var thermalCapacityPerHour = GetRoomThermalCapacityJPerK(room, preferences) / 3600.0;
            var totalHeatTransfer =
                outdoorUa + groundUa + ventilationHeatTransfer + adjacent.HeatTransferCoefficientWPerK;

            var baseBalance = thermalCapacityPerHour * previousRoomTemperature +
                              (outdoorUa + ventilationHeatTransfer) * weather.DryBulbTemperature +
                              groundUa * _options.DefaultGroundBoundaryTemperatureC +
                              adjacent.BoundaryTemperatureWeightedHeatTransferW +
                              internalGains +
                              solarGains;

            var freeFloatingTemperature = totalHeatTransfer > 0
                ? baseBalance / (thermalCapacityPerHour + totalHeatTransfer)
                : previousRoomTemperature;

            var heatingLoad = 0.0;
            var coolingLoad = 0.0;
            var operativeTemperature = freeFloatingTemperature;

            if (freeFloatingTemperature < heatingSetpoint)
            {
                heatingLoad = Math.Max(0, heatingSetpoint * (thermalCapacityPerHour + totalHeatTransfer) - baseBalance);
                heatingLoad *= preferences?.HeatingSafetyFactor ?? 1.0;
                operativeTemperature = heatingSetpoint;
            }
            else if (freeFloatingTemperature > coolingSetpoint)
            {
                coolingLoad = Math.Max(0, baseBalance - coolingSetpoint * (thermalCapacityPerHour + totalHeatTransfer));
                coolingLoad *= preferences?.CoolingSafetyFactor ?? 1.0;
                operativeTemperature = coolingSetpoint;
            }

            roomResults.Add(new RoomHourResult(
                room.Id,
                new Iso52016RoomHourlyEnergyNeed(
                    RoomId: room.Id,
                    RoomName: room.Name,
                    ZoneName: zone.Name,
                    HourOfYear: hourOfYear,
                    Month: GetMonth(hourOfYear),
                    HeatingLoadW: Round(heatingLoad),
                    CoolingLoadW: Round(coolingLoad),
                    OperativeTemperatureC: Round(operativeTemperature),
                    OutdoorTemperatureC: Round(weather.DryBulbTemperature),
                    InternalGainsW: Round(internalGains),
                    SolarGainsW: Round(solarGains))));
        }

        var zoneHeating = roomResults.Sum(x => x.Hour.HeatingLoadW);
        var zoneCooling = roomResults.Sum(x => x.Hour.CoolingLoadW);
        var zoneInternal = roomResults.Sum(x => x.Hour.InternalGainsW);
        var zoneSolar = roomResults.Sum(x => x.Hour.SolarGainsW);

        var totalArea = rooms.Sum(room => room.Area.SquareMeters);
        var zoneOperative = totalArea > 0
            ? rooms.Join(
                    roomResults,
                    room => room.Id,
                    result => result.RoomId,
                    (room, result) => room.Area.SquareMeters * result.Hour.OperativeTemperatureC)
                .Sum() / totalArea
            : roomResults.Average(x => x.Hour.OperativeTemperatureC);

        return new ZoneHourResult(
            zone.Name,
            new Iso52016HourlyEnergyNeed(
                HourOfYear: hourOfYear,
                Month: GetMonth(hourOfYear),
                HeatingLoadW: Round(zoneHeating),
                CoolingLoadW: Round(zoneCooling),
                OperativeTemperatureC: Round(zoneOperative),
                OutdoorTemperatureC: Round(weather.DryBulbTemperature),
                InternalGainsW: Round(zoneInternal),
                SolarGainsW: Round(zoneSolar)),
            roomResults);
    }

    private AdjacentBoundaryContribution CalculateAdjacentBoundaryContribution(
        IReadOnlyCollection<Room> rooms,
        double currentZoneReferenceTemperatureC,
        double outdoorTemperatureC,
        IReadOnlyDictionary<string, double> previousZoneTemperatures,
        IReadOnlyDictionary<int, string> roomZoneMap)
    {
        var totalHeatTransferCoefficient = 0.0;
        var weightedBoundaryTemperature = 0.0;

        foreach (var room in rooms)
        {
            foreach (var wall in room.Walls)
            {
                var heatTransferCoefficient = wall.Area.SquareMeters * GetWallUValue(wall);

                switch (wall.BoundaryType)
                {
                    case WallBoundaryType.Adiabatic:
                        continue;

                    case WallBoundaryType.AdjacentConditioned:
                    {
                        if (wall.AdjacentRoom is null)
                            continue;

                        if (IsAdiabaticAdjacentBoundary(room, wall.AdjacentRoom, roomZoneMap))
                            continue;

                        var boundaryTemperature = ResolveAdjacentConditionedTemperature(
                            wall.AdjacentRoom,
                            previousZoneTemperatures,
                            roomZoneMap);

                        totalHeatTransferCoefficient += heatTransferCoefficient;
                        weightedBoundaryTemperature += heatTransferCoefficient * boundaryTemperature;
                        break;
                    }

                    case WallBoundaryType.AdjacentUnconditioned:
                    {
                        var boundaryTemperature = outdoorTemperatureC +
                                                  (currentZoneReferenceTemperatureC - outdoorTemperatureC) *
                                                  Math.Clamp(_options.AdjacentUnconditionedTemperatureWeight, 0, 1);

                        totalHeatTransferCoefficient += heatTransferCoefficient;
                        weightedBoundaryTemperature += heatTransferCoefficient * boundaryTemperature;
                        break;
                    }

                    case WallBoundaryType.External:
                    case WallBoundaryType.Ground:
                    default:
                        continue;
                }
            }
        }

        return new AdjacentBoundaryContribution(
            HeatTransferCoefficientWPerK: totalHeatTransferCoefficient,
            BoundaryTemperatureWeightedHeatTransferW: weightedBoundaryTemperature);
    }

    private bool IsAdiabaticAdjacentBoundary(
        Room sourceRoom,
        Room adjacentRoom,
        IReadOnlyDictionary<int, string> roomZoneMap)
    {
        if (roomZoneMap.TryGetValue(sourceRoom.Id, out var sourceZone) &&
            roomZoneMap.TryGetValue(adjacentRoom.Id, out var adjacentZone) &&
            string.Equals(sourceZone, adjacentZone, StringComparison.Ordinal))
        {
            return true;
        }

        return _options.TreatSameUseAdjacentConditionedAsAdiabatic &&
               sourceRoom.Type == adjacentRoom.Type;
    }

    private static double ResolveAdjacentConditionedTemperature(
        Room adjacentRoom,
        IReadOnlyDictionary<string, double> previousZoneTemperatures,
        IReadOnlyDictionary<int, string> roomZoneMap)
    {
        if (roomZoneMap.TryGetValue(adjacentRoom.Id, out var adjacentZone) &&
            previousZoneTemperatures.TryGetValue(adjacentZone, out var adjacentZoneTemperature))
        {
            return adjacentZoneTemperature;
        }

        return adjacentRoom.IndoorTemperature.Celsius;
    }

    private ThermalZoneState CreateThermalZoneState(
        ThermalZoneGroup zone,
        CalculationPreferences? preferences,
        double? coolingSetpointOverride = null)
    {
        var rooms = zone.Rooms;
        var envelopeDefaults = _envelopeReferenceData.GetDefaults();
        var floorArea = rooms.Sum(room => room.Area.SquareMeters);
        var volume = rooms.Sum(room => room.CalculateVolume());

        var outdoorEnvelope = rooms.Sum(room => GetOutdoorEnvelopeHeatTransferCoefficient(room, envelopeDefaults));
        var groundEnvelope = rooms.Sum(room => GetGroundEnvelopeHeatTransferCoefficient(room, envelopeDefaults));
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

        var envelopeCapacity = rooms.Sum(room => room.CalculateInternalHeatCapacityKjPerK(
            envelopeDefaults.FloorHeatCapacityKjPerM2K,
            envelopeDefaults.CeilingHeatCapacityKjPerM2K) * 1000.0);

        var thermalCapacity = Math.Max(
            envelopeCapacity + floorArea * GetInternalHeatCapacityJPerM2K(preferences),
            Math.Max(floorArea, 1.0) * GetInternalHeatCapacityJPerM2K(preferences));

        var heatingSetpoint = WeightedAverage(rooms, room => room.IndoorTemperature.Celsius);
        var coolingSetpoint = coolingSetpointOverride ?? Math.Max(_options.DefaultCoolingSetpointC, heatingSetpoint);

        return new ThermalZoneState(
            FloorAreaM2: floorArea,
            VolumeM3: volume,
            OutdoorBoundaryHeatTransferCoefficientWPerK: outdoorEnvelope,
            GroundBoundaryHeatTransferCoefficientWPerK: groundEnvelope,
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
        const double defaultMinimumDirectSolarReductionFactor = 1.0;
        const double defaultDiffuseSolarShareUnaffected = 0.3;

        var hasWindowSpecificShading =
            shading.OverhangDepthM > 0 ||
            shading.SideFinDepthM > 0 ||
            shading.RevealDepthM > 0 ||
            shading.WindowHeightM > 0 ||
            shading.WindowWidthM > 0 ||
            Math.Abs(shading.MinimumDirectSolarReductionFactor - defaultMinimumDirectSolarReductionFactor) > 0.0001 ||
            Math.Abs(shading.DiffuseSolarShareUnaffected - defaultDiffuseSolarShareUnaffected) > 0.0001;

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
        var scheduleFactor = WeightedAverage(rooms, room => GetVentilationFactor(room, hourOfDay));

        var natural = rooms.Sum(room =>
            _naturalVentilationAirflowService?.CalculateHeatTransferCoefficient(
                room,
                state.HeatingSetpointC,
                outdoorTemperatureC,
                windSpeedMPerS,
                GetVentilationFactor(room, hourOfDay),
                hourOfDay) ?? 0.0);

        if (_ventilationCalculator is null)
            return state.VentilationHeatTransferCoefficientWPerK * scheduleFactor + natural;

        var infiltration = rooms.Sum(room => _ventilationCalculator.CalculateInfiltration(
            room,
            new VentilationCalculationContext(
                VentilationCalculationMethod.TemperatureWind,
                state.HeatingSetpointC,
                outdoorTemperatureC,
                windSpeedMPerS)));

        var mechanical = scheduleFactor <= 0
            ? rooms.Sum(room => _ventilationCalculator.CalculateMechanical(
                room,
                new VentilationCalculationContext(
                    VentilationCalculationMethod.FixedAirChanges,
                    state.HeatingSetpointC,
                    outdoorTemperatureC)))
            : rooms.Sum(room => _ventilationCalculator.CalculateMechanical(
                room,
                new VentilationCalculationContext(
                    VentilationCalculationMethod.Occupancy,
                    state.HeatingSetpointC,
                    outdoorTemperatureC))) * scheduleFactor;

        return infiltration + mechanical + natural;
    }

    private double GetHourlyInternalGain(Room room, int hour)
    {
        var people = room.PeopleCount * GetPeopleHeatGain(room.Type) * GetOccupancyFactor(room, hour);
        var equipment = room.EquipmentLoad.Watts * GetEquipmentFactor(room, hour);
        var lighting = room.LightingLoad.Watts * GetLightingFactor(room, hour);

        return people + equipment + lighting;
    }

    private double GetOccupancyFactor(Room room, int hour) =>
        GetScheduleOrProfileFactor(room, room.OccupancySchedule, hour, profile => profile.OccupancyFactors);

    private double GetEquipmentFactor(Room room, int hour) =>
        GetScheduleOrProfileFactor(room, room.EquipmentSchedule, hour, profile => profile.EquipmentFactors);

    private double GetLightingFactor(Room room, int hour) =>
        GetScheduleOrProfileFactor(room, room.LightingSchedule, hour, profile => profile.LightingFactors);

    private double GetVentilationFactor(Room room, int hour) =>
        GetScheduleOrProfileFactor(room, null, hour, profile => profile.VentilationFactors);

    private double GetScheduleOrProfileFactor(
        Room room,
        HourlySchedule? schedule,
        int hour,
        Func<Models.ReferenceData.En16798RoomUsageProfile, IReadOnlyList<double>> selector)
    {
        if (schedule?.Factors.Count == 24)
            return schedule.Factors[hour];

        if (!_profileOptions.UseStandardProfilesWhenMissingSchedules)
            return 1.0;

        var profile = _profileCatalog.GetProfile(room.Type, _profileOptions.DefaultCategory);
        return selector(profile)[hour];
    }

    private static double GetOutdoorEnvelopeHeatTransferCoefficient(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.External)
            .Sum(wall => wall.Area.SquareMeters * GetWallUValue(wall));

        envelope += room.Windows.Sum(window => window.Area.SquareMeters * window.UValue.Value);
        envelope += room.Area.SquareMeters * envelopeDefaults.CeilingUValueWPerM2K;

        return envelope;
    }

    private static double GetGroundEnvelopeHeatTransferCoefficient(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.Ground)
            .Sum(wall => wall.Area.SquareMeters * GetWallUValue(wall));

        envelope += room.Area.SquareMeters * envelopeDefaults.FloorUValueWPerM2K;

        return envelope;
    }

    private static IReadOnlyList<ThermalZoneGroup> GetThermalZoneGroups(Building building)
    {
        var allRooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToArray();

        if (building.ThermalZones.Count == 0)
            return [new ThermalZoneGroup("Building", allRooms)];

        var countedRooms = new HashSet<Room>();
        var groups = new List<ThermalZoneGroup>();

        foreach (var zone in building.ThermalZones.OrderBy(zone => zone.Id))
        {
            var zoneRooms = zone.AssignedRooms
                .Where(countedRooms.Add)
                .ToArray();

            if (zoneRooms.Length > 0)
                groups.Add(new ThermalZoneGroup(zone.Name, zoneRooms));
        }

        var unassignedRooms = allRooms
            .Where(room => !countedRooms.Contains(room))
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

    private static double WeightedAverage(IReadOnlyCollection<Room> rooms, Func<Room, double> valueSelector)
    {
        var totalArea = rooms.Sum(room => room.Area.SquareMeters);
        if (totalArea <= 0)
            return rooms.Count == 0 ? 0 : rooms.Average(valueSelector);

        return rooms.Sum(room => valueSelector(room) * room.Area.SquareMeters) / totalArea;
    }

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
        double OutdoorBoundaryHeatTransferCoefficientWPerK,
        double GroundBoundaryHeatTransferCoefficientWPerK,
        double VentilationHeatTransferCoefficientWPerK,
        double ThermalCapacityJPerK,
        double HeatingSetpointC,
        double CoolingSetpointC);

    private sealed record AdjacentBoundaryContribution(
        double HeatTransferCoefficientWPerK,
        double BoundaryTemperatureWeightedHeatTransferW);

    private sealed record ZoneHourResult(
        string ZoneName,
        Iso52016HourlyEnergyNeed Hour,
        IReadOnlyCollection<RoomHourResult> Rooms);
    
    private sealed record RoomHourResult(
        int RoomId,
        Iso52016RoomHourlyEnergyNeed Hour);

    private double GetHourlyVentilationHeatTransferForRoom(
        Room room,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeedMPerS,
        int hourOfDay)
    {
        var scheduleFactor = GetVentilationFactor(room, hourOfDay);

        var natural = _naturalVentilationAirflowService?.CalculateHeatTransferCoefficient(
            room,
            indoorTemperatureC,
            outdoorTemperatureC,
            windSpeedMPerS,
            scheduleFactor,
            hourOfDay) ?? 0.0;

        if (_ventilationCalculator is null)
            return natural;

        var infiltration = _ventilationCalculator.CalculateInfiltration(
            room,
            new VentilationCalculationContext(
                VentilationCalculationMethod.TemperatureWind,
                indoorTemperatureC,
                outdoorTemperatureC,
                windSpeedMPerS));

        var mechanical = scheduleFactor <= 0
            ? _ventilationCalculator.CalculateMechanical(
                room,
                new VentilationCalculationContext(
                    VentilationCalculationMethod.FixedAirChanges,
                    indoorTemperatureC,
                    outdoorTemperatureC))
            : _ventilationCalculator.CalculateMechanical(
                room,
                new VentilationCalculationContext(
                    VentilationCalculationMethod.Occupancy,
                    indoorTemperatureC,
                    outdoorTemperatureC)) * scheduleFactor;

        return infiltration + mechanical + natural;
    }

    private AdjacentBoundaryContribution CalculateAdjacentBoundaryContributionForRoom(
        Room room,
        double currentRoomReferenceTemperatureC,
        double outdoorTemperatureC,
        IReadOnlyDictionary<int, double> previousRoomTemperatures,
        IReadOnlyDictionary<int, string> roomZoneMap)
    {
        var totalHeatTransferCoefficient = 0.0;
        var weightedBoundaryTemperature = 0.0;

        foreach (var wall in room.Walls)
        {
            var heatTransferCoefficient = wall.Area.SquareMeters * GetWallUValue(wall);

            switch (wall.BoundaryType)
            {
                case WallBoundaryType.Adiabatic:
                    continue;

                case WallBoundaryType.AdjacentConditioned:
                {
                    if (wall.AdjacentRoom is null)
                        continue;

                    if (IsAdiabaticAdjacentBoundary(room, wall.AdjacentRoom, roomZoneMap))
                        continue;

                    var boundaryTemperature =
                        previousRoomTemperatures.TryGetValue(wall.AdjacentRoom.Id, out var adjacentTemp)
                            ? adjacentTemp
                            : wall.AdjacentRoom.IndoorTemperature.Celsius;

                    totalHeatTransferCoefficient += heatTransferCoefficient;
                    weightedBoundaryTemperature += heatTransferCoefficient * boundaryTemperature;
                    break;
                }

                case WallBoundaryType.AdjacentUnconditioned:
                {
                    var boundaryTemperature = outdoorTemperatureC +
                                              (currentRoomReferenceTemperatureC - outdoorTemperatureC) *
                                              Math.Clamp(_options.AdjacentUnconditionedTemperatureWeight, 0, 1);

                    totalHeatTransferCoefficient += heatTransferCoefficient;
                    weightedBoundaryTemperature += heatTransferCoefficient * boundaryTemperature;
                    break;
                }

                case WallBoundaryType.External:
                case WallBoundaryType.Ground:
                default:
                    continue;
            }
        }

        return new AdjacentBoundaryContribution(
            HeatTransferCoefficientWPerK: totalHeatTransferCoefficient,
            BoundaryTemperatureWeightedHeatTransferW: weightedBoundaryTemperature);
    }

    private double GetRoomThermalCapacityJPerK(Room room, CalculationPreferences? preferences)
    {
        var defaults = _envelopeReferenceData.GetDefaults();
        var envelopeCapacity = room.CalculateInternalHeatCapacityKjPerK(
            defaults.FloorHeatCapacityKjPerM2K,
            defaults.CeilingHeatCapacityKjPerM2K) * 1000.0;

        return Math.Max(
            envelopeCapacity + room.Area.SquareMeters * GetInternalHeatCapacityJPerM2K(preferences),
            Math.Max(room.Area.SquareMeters, 1.0) * GetInternalHeatCapacityJPerM2K(preferences));
    }
}