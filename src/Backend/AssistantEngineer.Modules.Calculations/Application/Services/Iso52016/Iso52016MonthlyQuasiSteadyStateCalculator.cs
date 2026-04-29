using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016MonthlyQuasiSteadyStateCalculator
{
    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private readonly IAnnualClimateDataProvider _climateDataProvider;
    private readonly ISolarRadiationService _solarRadiationService;
    private readonly IWindowShadingService _windowShadingService;
    private readonly IVentilationHeatTransferCalculator _ventilationCalculator;
    private readonly IBuildingEnvelopeReferenceData _buildingEnvelopeReferenceData;
    private readonly IEn16798ProfileCatalog _profileCatalog;
    private readonly INaturalVentilationAirflowService _naturalVentilationAirflowService;
    private readonly Iso52016EnergyNeedOptions _energyOptions;
    private readonly Iso52016MonthlyEnergyNeedOptions _monthlyOptions;
    private readonly En16798ProfileOptions _profileOptions;
    private readonly IGroundTemperatureService _groundTemperatureService;
    private readonly IGroundHeatTransferService _groundHeatTransferService;
    private readonly WindowSolarGainEngine _windowSolarGains;

    public Iso52016MonthlyQuasiSteadyStateCalculator(
        IAnnualClimateDataProvider climateDataProvider,
        ISolarRadiationService solarRadiationService,
        IWindowShadingService windowShadingService,
        IVentilationHeatTransferCalculator ventilationCalculator,
        IBuildingEnvelopeReferenceData buildingEnvelopeReferenceData,
        IEn16798ProfileCatalog profileCatalog,
        INaturalVentilationAirflowService naturalVentilationAirflowService,
        IOptions<Iso52016EnergyNeedOptions> energyOptions,
        IOptions<Iso52016MonthlyEnergyNeedOptions> monthlyOptions,
        IOptions<En16798ProfileOptions> profileOptions,
        IGroundTemperatureService groundTemperatureService,
        IGroundHeatTransferService groundHeatTransferService,
        WindowSolarGainEngine? windowSolarGains = null)
    {
        _climateDataProvider = climateDataProvider;
        _solarRadiationService = solarRadiationService;
        _windowShadingService = windowShadingService;
        _ventilationCalculator = ventilationCalculator;
        _buildingEnvelopeReferenceData = buildingEnvelopeReferenceData;
        _profileCatalog = profileCatalog;
        _naturalVentilationAirflowService = naturalVentilationAirflowService;
        _energyOptions = energyOptions.Value;
        _monthlyOptions = monthlyOptions.Value;
        _profileOptions = profileOptions.Value;
        _groundTemperatureService = groundTemperatureService;
        _groundHeatTransferService = groundHeatTransferService;
        _windowSolarGains = windowSolarGains ?? new WindowSolarGainEngine();
    }

    public async Task<Iso52016AnnualEnergyNeedResult?> CalculateBuildingEnergyNeedsAsync(
        Building building,
        CalculationPreferences? preferences = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var climateZone = building.ClimateZone
            ?? throw new InvalidOperationException("Building must have a climate zone assigned.");

        var effectiveYear = year ?? _energyOptions.DefaultWeatherYear;
        var annualData = await _climateDataProvider.GetForClimateZoneAsync(
            climateZone.Id,
            effectiveYear,
            cancellationToken);

        if (!HasCompleteAnnualWeatherData(annualData))
            return null;

        var orderedHourlyData = annualData!.HourlyData
            .Where(hour => hour.HourOfYear.HasValue)
            .OrderBy(hour => hour.HourOfYear!.Value)
            .ToArray();

        var zoneGroups = GetThermalZoneGroups(building);
        var zoneMonthResults = new List<MonthlyZoneEnergy>();

        foreach (var zone in zoneGroups)
        {
            var groupedMonths = orderedHourlyData
                .GroupBy(hour => GetMonth(hour.HourOfYear!.Value))
                .OrderBy(group => group.Key);

            foreach (var monthGroup in groupedMonths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var monthlyGroundTemperature = _groundTemperatureService.GetMonthlyAverageTemperature(
                    orderedHourlyData,
                    monthGroup.Key);

                var monthlyZoneResult = CalculateZoneMonth(
                    zone,
                    monthGroup.Key,
                    monthGroup.ToArray(),
                    preferences,
                    monthlyGroundTemperature);

                zoneMonthResults.Add(monthlyZoneResult);
            }
        }

        var monthlyResults = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var rows = zoneMonthResults.Where(result => result.Month == month).ToArray();
                return new Iso52016MonthlyEnergyNeed(
                    Month: month,
                    HeatingDemandKWh: Round(rows.Sum(result => result.HeatingDemandKWh)),
                    CoolingDemandKWh: Round(rows.Sum(result => result.CoolingDemandKWh)));
            })
            .ToArray();

        var annualHeating = Round(monthlyResults.Sum(month => month.HeatingDemandKWh));
        var annualCooling = Round(monthlyResults.Sum(month => month.CoolingDemandKWh));
        var breakdown = new Iso52016EnergyBalanceBreakdown(
            SolarGainsKWh: Round(zoneMonthResults.Sum(result => result.SolarGainsKWh)),
            InternalGainsKWh: Round(zoneMonthResults.Sum(result => result.InternalGainsKWh)),
            HeatingInputKWh: annualHeating,
            CoolingExtractedKWh: annualCooling);

        return new Iso52016AnnualEnergyNeedResult(
            BuildingId: building.Id,
            BuildingName: building.Name,
            Year: annualData.Year,
            HourlyResults: Array.Empty<Iso52016HourlyEnergyNeed>(),
            MonthlyResults: monthlyResults,
            AnnualHeatingDemandKWh: annualHeating,
            AnnualCoolingDemandKWh: annualCooling,
            Breakdown: breakdown);
    }

    private MonthlyZoneEnergy CalculateZoneMonth(
        ThermalZoneGroup zone,
        int month,
        IReadOnlyList<HourlyClimateData> monthHours,
        CalculationPreferences? preferences,
        double groundBoundaryTemperatureC)
    {
        var rooms = zone.Rooms;
        var envelopeDefaults = _buildingEnvelopeReferenceData.GetDefaults();
        var transmissionHeatTransfer = rooms.Sum(room => GetTransmissionHeatTransferCoefficient(room, envelopeDefaults));

        var occupiedFraction = WeightedAverage(rooms, room =>
            AverageScheduleFactor(hour => GetOccupancyFactor(room, hour), monthHours));

        var heatingSetpointOccupied = WeightedAverage(rooms, room => room.IndoorTemperature.Celsius);
        var coolingSetpointOccupied = Math.Max(_energyOptions.DefaultCoolingSetpointC, heatingSetpointOccupied);

        var effectiveHeatingSetpoint = occupiedFraction * heatingSetpointOccupied +
            (1 - occupiedFraction) * _energyOptions.DefaultHeatingSetbackC;

        var effectiveCoolingSetpoint = occupiedFraction * coolingSetpointOccupied +
            (1 - occupiedFraction) * _energyOptions.DefaultCoolingSetbackC;

        var averageOutdoorTemperature = monthHours.Average(hour => hour.DryBulbTemperature);

        var averageVentilationHeatTransfer = monthHours.Average(hour =>
            GetHourlyVentilationHeatTransfer(
                rooms,
                effectiveHeatingSetpoint,
                hour.DryBulbTemperature,
                hour.WindSpeedMPerS ?? 0,
                hour.HourOfYear!.Value % 24));

        var outdoorUa = rooms.Sum(room => GetOutdoorTransmissionHeatTransferCoefficient(room, envelopeDefaults));
        var groundBoundaryConditions = rooms
            .Select(room => _groundHeatTransferService.CalculateBoundaryCondition(room, envelopeDefaults))
            .ToArray();
        var groundUa = groundBoundaryConditions.Sum(x => x.HeatTransferCoefficientWPerK);var hours = monthHours.Count;

        var internalGainsKWh = monthHours.Sum(hour =>
            rooms.Sum(room => GetHourlyInternalGain(room, hour.HourOfYear!.Value % 24))) / 1000.0;

        var solarGainsKWh = monthHours.Sum(hour =>
            rooms.Sum(room => GetHourlySolarGain(room, hour, preferences))) / 1000.0;

        var totalUsefulGainsKWh = internalGainsKWh + solarGainsKWh;

        var equivalentGroundBoundaryTemperature = groundUa <= 0
            ? groundBoundaryTemperatureC
            : groundBoundaryConditions.Sum(x =>
                x.HeatTransferCoefficientWPerK *
                (x.IndoorTemperatureWeight * effectiveHeatingSetpoint +
                 x.OutdoorTemperatureWeight * averageOutdoorTemperature +
                 x.GroundTemperatureWeight * groundBoundaryTemperatureC)) / groundUa;
        
        var heatingLossesKWh = Math.Max(
            0,
            ((outdoorUa * Math.Max(0, effectiveHeatingSetpoint - averageOutdoorTemperature)) +
             (groundUa * Math.Max(0, effectiveHeatingSetpoint - equivalentGroundBoundaryTemperature)) +
             (averageVentilationHeatTransfer * Math.Max(0, effectiveHeatingSetpoint - averageOutdoorTemperature)))
            * hours / 1000.0);

        var heatingDemandKWh = Math.Max(
            0,
            heatingLossesKWh - totalUsefulGainsKWh * _monthlyOptions.HeatingGainUtilizationFactor);

        var equivalentCoolingGroundBoundaryTemperature = groundUa <= 0
            ? groundBoundaryTemperatureC
            : groundBoundaryConditions.Sum(x =>
                x.HeatTransferCoefficientWPerK *
                (x.IndoorTemperatureWeight * effectiveCoolingSetpoint +
                 x.OutdoorTemperatureWeight * averageOutdoorTemperature +
                 x.GroundTemperatureWeight * groundBoundaryTemperatureC)) / groundUa;

        var coolingTransmissionKWh = Math.Max(
            0,
            ((outdoorUa * Math.Max(0, averageOutdoorTemperature - effectiveCoolingSetpoint)) +
             (groundUa * Math.Max(0, equivalentCoolingGroundBoundaryTemperature - effectiveCoolingSetpoint)) +
             (averageVentilationHeatTransfer * Math.Max(0, averageOutdoorTemperature - effectiveCoolingSetpoint)))
            * hours / 1000.0);
        
        var coolingDemandKWh = Math.Max(
            0,
            coolingTransmissionKWh + totalUsefulGainsKWh * _monthlyOptions.CoolingGainUtilizationFactor);

        heatingDemandKWh *= preferences?.HeatingSafetyFactor ?? 1.0;
        coolingDemandKWh *= preferences?.CoolingSafetyFactor ?? 1.0;

        if (heatingDemandKWh < _monthlyOptions.MinimumMonthlyDemandKWh)
            heatingDemandKWh = 0;

        if (coolingDemandKWh < _monthlyOptions.MinimumMonthlyDemandKWh)
            coolingDemandKWh = 0;

        return new MonthlyZoneEnergy(
            Month: month,
            HeatingDemandKWh: Round(heatingDemandKWh),
            CoolingDemandKWh: Round(coolingDemandKWh),
            InternalGainsKWh: Round(internalGainsKWh),
            SolarGainsKWh: Round(solarGainsKWh));
    }

    private double GetHourlySolarGain(
        Room room,
        HourlyClimateData weather,
        CalculationPreferences? preferences)
    {
        var hourOfYear = weather.HourOfYear!.Value;
        var hourOfDay = hourOfYear % 24;
        var dayOfYear = hourOfYear / 24 + 1;

        return room.Windows.Sum(window =>
        {
            var radiation = _solarRadiationService.CalculateVerticalSurfaceRadiation(
                weather,
                window.Orientation,
                _energyOptions.LatitudeDegrees,
                dayOfYear,
                hourOfDay);

            var shadingReduction = GetWindowShadingReduction(window, dayOfYear, hourOfDay, preferences);

            var solar = _windowSolarGains.Calculate(
                WindowSolarGainInputFactory.CreateForWindow(
                    window,
                    radiation,
                    frameFactor: 1 - GetWindowFrameAreaFraction(preferences),
                    externalShadingFactor: shadingReduction,
                    fixedShadingFactor: GetSolarUtilizationFactor(preferences),
                    hourIndex: weather.HourOfYear));

            return solar.Value.SolarGainW;
        });
    }

    private double GetWindowShadingReduction(
        Window window,
        int dayOfYear,
        int hourOfDay,
        CalculationPreferences? preferences)
    {
        var configuredReduction = Math.Clamp(GetDirectSolarShadingReductionFactor(preferences), 0, 1);

        var shading = window.Shading;
        var hasWindowSpecificShading =
            shading.OverhangDepthM > 0 ||
            shading.SideFinDepthM > 0 ||
            shading.RevealDepthM > 0 ||
            shading.WindowHeightM > 0 ||
            shading.WindowWidthM > 0 ||
            Math.Abs(shading.MinimumDirectSolarReductionFactor - 1.0) > 0.0001 ||
            Math.Abs(shading.DiffuseSolarShareUnaffected - 0.3) > 0.0001;

        var windowHeight = shading.WindowHeightM > 0
            ? shading.WindowHeightM
            : _energyOptions.DefaultWindowHeightM;

        var windowWidth = shading.WindowWidthM > 0
            ? shading.WindowWidthM
            : _energyOptions.DefaultWindowWidthM;

        var geometricReduction = _windowShadingService.CalculateCombinedSolarReduction(
            window.Orientation,
            _energyOptions.LatitudeDegrees,
            dayOfYear,
            hourOfDay,
            new WindowShadingOptions(
                shading.OverhangDepthM > 0 ? shading.OverhangDepthM : 0,
                shading.SideFinDepthM > 0 ? shading.SideFinDepthM : 0,
                shading.RevealDepthM > 0 ? shading.RevealDepthM : 0,
                windowHeight,
                windowWidth,
                hasWindowSpecificShading
                    ? shading.MinimumDirectSolarReductionFactor
                    : _energyOptions.MinimumDirectSolarShadingReductionFactor,
                hasWindowSpecificShading
                    ? shading.DiffuseSolarShareUnaffected
                    : GetDiffuseSolarShareUnaffectedByShading(preferences)));

        return Math.Clamp(configuredReduction * geometricReduction, 0, 1);
    }

    private double GetHourlyVentilationHeatTransfer(
        IReadOnlyCollection<Room> rooms,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeedMPerS,
        int hourOfDay)
    {
        var scheduleFactor = WeightedAverage(rooms, room => GetVentilationFactor(room, hourOfDay));

        var infiltration = rooms.Sum(room => _ventilationCalculator.CalculateInfiltration(
            room,
            new VentilationCalculationContext(
                VentilationCalculationMethod.TemperatureWind,
                indoorTemperatureC,
                outdoorTemperatureC,
                windSpeedMPerS)));

        var mechanical = scheduleFactor <= 0
            ? rooms.Sum(room => _ventilationCalculator.CalculateMechanical(
                room,
                new VentilationCalculationContext(
                    VentilationCalculationMethod.FixedAirChanges,
                    indoorTemperatureC,
                    outdoorTemperatureC)))
            : rooms.Sum(room => _ventilationCalculator.CalculateMechanical(
                room,
                new VentilationCalculationContext(
                    VentilationCalculationMethod.Occupancy,
                    indoorTemperatureC,
                    outdoorTemperatureC))) * scheduleFactor;

        var natural = rooms.Sum(room =>
            _naturalVentilationAirflowService.CalculateHeatTransferCoefficient(
                room,
                indoorTemperatureC,
                outdoorTemperatureC,
                windSpeedMPerS,
                GetVentilationFactor(room, hourOfDay),
                hourOfDay));

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

    private static double AverageScheduleFactor(
        Func<int, double> factorResolver,
        IReadOnlyList<HourlyClimateData> monthHours) =>
        monthHours.Average(hour => factorResolver(hour.HourOfYear!.Value % 24));

    private double GetSolarUtilizationFactor(CalculationPreferences? preferences) =>
        preferences is null
            ? _energyOptions.DefaultSolarUtilizationFactor
            : Math.Clamp(preferences.Iso52016SolarUtilizationFactor, 0, 1);

    private double GetWindowFrameAreaFraction(CalculationPreferences? preferences) =>
        preferences is null
            ? _energyOptions.DefaultWindowFrameAreaFraction
            : Math.Clamp(preferences.Iso52016WindowFrameAreaFraction, 0, 0.9);

    private double GetDirectSolarShadingReductionFactor(CalculationPreferences? preferences) =>
        preferences is null
            ? _energyOptions.DefaultDirectSolarShadingReductionFactor
            : Math.Clamp(preferences.Iso52016DirectSolarShadingReductionFactor, 0, 1);

    private double GetDiffuseSolarShareUnaffectedByShading(CalculationPreferences? preferences) =>
        preferences is null
            ? _energyOptions.DiffuseSolarShareUnaffectedByShading
            : Math.Clamp(preferences.Iso52016DiffuseSolarShareUnaffectedByShading, 0, 1);

    private static double GetTransmissionHeatTransferCoefficient(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.External)
            .Sum(wall => wall.Area.SquareMeters * GetWallUValue(wall));

        envelope += room.Windows.Sum(window => window.Area.SquareMeters * window.UValue.Value);
        envelope += room.Area.SquareMeters * envelopeDefaults.FloorUValueWPerM2K;
        envelope += room.Area.SquareMeters * envelopeDefaults.CeilingUValueWPerM2K;

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

    private static int GetMonth(int hourOfYear)
    {
        var dayOfYear = hourOfYear / 24;
        var accumulatedDays = 0;
        foreach (var entry in DaysPerMonth.Select((days, index) => new { Month = index + 1, Days = days }))
        {
            accumulatedDays += entry.Days;
            if (dayOfYear < accumulatedDays)
                return entry.Month;
        }

        return 12;
    }

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

    private static double Round(double value) =>
        Math.Round(Math.Max(0, value), 2, MidpointRounding.AwayFromZero);

    private sealed record ThermalZoneGroup(string Name, IReadOnlyCollection<Room> Rooms);

    private sealed record MonthlyZoneEnergy(
        int Month,
        double HeatingDemandKWh,
        double CoolingDemandKWh,
        double InternalGainsKWh,
        double SolarGainsKWh);
    
    private static double GetOutdoorTransmissionHeatTransferCoefficient(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.External)
            .Sum(wall => wall.Area.SquareMeters * Iso52016HourlyCalculatorMath.GetWallUValue(wall));

        envelope += room.Windows.Sum(window => window.Area.SquareMeters * window.UValue.Value);
        envelope += room.Area.SquareMeters * envelopeDefaults.CeilingUValueWPerM2K;

        return envelope;
    }

    private static double GetGroundTransmissionHeatTransferCoefficient(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.Ground)
            .Sum(wall => wall.Area.SquareMeters * Iso52016HourlyCalculatorMath.GetWallUValue(wall));

        envelope += room.Area.SquareMeters * envelopeDefaults.FloorUValueWPerM2K;

        return envelope;
    }
}
