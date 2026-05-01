using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analysis;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Ground;
using AssistantEngineer.Modules.Calculations.Application.Models.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal sealed class Iso52016HourlyHeatBalanceCalculator
{
    private readonly ISolarRadiationService _solarRadiationService;
    private readonly IVentilationHeatTransferCalculator? _ventilationCalculator;
    private readonly IWindowShadingService? _windowShadingService;
    private readonly IBuildingEnvelopeReferenceData _envelopeReferenceData;
    private readonly IEn16798ProfileCatalog _profileCatalog;
    private readonly INaturalVentilationAirflowService? _naturalVentilationAirflowService;
    private readonly Iso52016EnergyNeedOptions _options;
    private readonly En16798ProfileOptions _profileOptions;
    private readonly HourlyInternalGainProfileService _hourlyProfiles;
    private readonly IGroundHeatTransferService _groundHeatTransferService;
    private readonly WindowSolarGainEngine _windowSolarGains;

    public Iso52016HourlyHeatBalanceCalculator(
        ISolarRadiationService solarRadiationService,
        IVentilationHeatTransferCalculator? ventilationCalculator,
        IWindowShadingService? windowShadingService,
        IBuildingEnvelopeReferenceData envelopeReferenceData,
        IEn16798ProfileCatalog profileCatalog,
        IGroundHeatTransferService groundHeatTransferService,
        INaturalVentilationAirflowService? naturalVentilationAirflowService,
        Iso52016EnergyNeedOptions options,
        En16798ProfileOptions profileOptions,
        HourlyInternalGainProfileService hourlyProfiles,
        WindowSolarGainEngine? windowSolarGains = null)
    {
        _solarRadiationService = solarRadiationService;
        _ventilationCalculator = ventilationCalculator;
        _windowShadingService = windowShadingService;
        _envelopeReferenceData = envelopeReferenceData;
        _profileCatalog = profileCatalog;
        _groundHeatTransferService = groundHeatTransferService;
        _naturalVentilationAirflowService = naturalVentilationAirflowService;
        _options = options;
        _profileOptions = profileOptions;
        _hourlyProfiles = hourlyProfiles;
        _windowSolarGains = windowSolarGains ?? new WindowSolarGainEngine();
    }

    public Iso52016ZoneHourResult CalculateZoneHourEnergyNeed(
        Iso52016ThermalZoneGroup zone,
        Iso52016ThermalZoneState state,
        AnnualHourlyData weather,
        IReadOnlyDictionary<int, double> previousRoomTemperatures,
        IReadOnlyDictionary<int, string> roomZoneMap,
        CalculationPreferences? preferences,
        CancellationToken cancellationToken,
        AnnualProfileOptionsDto? annualProfileOptions = null,
        double? groundBoundaryTemperatureC = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rooms = zone.Rooms;
        var hourOfYear = weather.HourOfYear;
        var hourOfDay = hourOfYear % 24;
        var dayOfYear = hourOfYear / 24 + 1;

        Dictionary<int, RoomHourlyProfileSnapshot>? annualSnapshots = null;
        if (annualProfileOptions?.UseAnnualProfiles == true)
        {
            annualSnapshots = rooms.ToDictionary(
                room => room.Id,
                room => _hourlyProfiles.GetRoomHourlyMultipliers(
                    room,
                    hourOfYear,
                    annualProfileOptions));
        }

        var occupiedScheduleFactor = Iso52016HourlyCalculatorMath.WeightedAverage(
            rooms,
            room =>
            {
                if (annualSnapshots is not null &&
                    annualSnapshots.TryGetValue(room.Id, out var snapshot))
                {
                    return snapshot.Occupancy;
                }

                return GetOccupancyFactor(room, hourOfDay);
            });

        var heatingSetpoint = occupiedScheduleFactor > 0
            ? state.HeatingSetpointC
            : _options.DefaultHeatingSetbackC;

        var coolingSetpoint = occupiedScheduleFactor > 0
            ? state.CoolingSetpointC
            : _options.DefaultCoolingSetbackC;

        var roomResults = new List<Iso52016RoomHourResult>(rooms.Count);

        foreach (var room in rooms)
        {
            var previousRoomTemperature = previousRoomTemperatures.TryGetValue(room.Id, out var previousTemp)
                ? previousTemp
                : room.IndoorTemperature.Celsius;

            RoomHourlyProfileSnapshot? profileSnapshot = null;

            if (annualSnapshots is not null)
                annualSnapshots.TryGetValue(room.Id, out profileSnapshot);

            var internalGains = GetHourlyInternalGain(
                room,
                hourOfDay,
                annualProfileOptions,
                profileSnapshot);

            var solarGains = GetHourlySolarGain(room, weather, dayOfYear, hourOfDay, preferences);

            var ventilationComponents = GetHourlyVentilationComponentsForRoom(
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

            var envelopeDefaults = _envelopeReferenceData.GetDefaults();
            var outdoorUa = GetOutdoorEnvelopeHeatTransferCoefficient(room, envelopeDefaults);
            var groundBoundary = _groundHeatTransferService.CalculateBoundaryCondition(room, envelopeDefaults);
            var groundUa = groundBoundary.HeatTransferCoefficientWPerK;
            var groundTemperature = groundBoundaryTemperatureC ?? _options.DefaultGroundBoundaryTemperatureC;

            var groundBoundaryReferenceTemperature = CalculateGroundBoundaryReferenceTemperature(
                groundBoundary,
                heatingSetpoint,
                weather.DryBulbTemperature,
                groundTemperature);

            var thermalCapacityPerHour = GetRoomThermalCapacityJPerK(room, preferences) / 3600.0;

            var totalHeatTransfer =
                outdoorUa +
                groundUa +
                ventilationComponents.TotalVentilationHeatTransferWPerK +
                ventilationComponents.InfiltrationHeatTransferWPerK +
                adjacent.HeatTransferCoefficientWPerK;

            var baseBalance =
                thermalCapacityPerHour * previousRoomTemperature +
                outdoorUa * weather.DryBulbTemperature +
                ventilationComponents.TotalVentilationHeatTransferWPerK * weather.DryBulbTemperature +
                ventilationComponents.InfiltrationHeatTransferWPerK * weather.DryBulbTemperature +
                groundUa * groundBoundaryReferenceTemperature +
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
                heatingLoad = Math.Max(
                    0,
                    heatingSetpoint * (thermalCapacityPerHour + totalHeatTransfer) - baseBalance);

                heatingLoad *= preferences?.HeatingSafetyFactor ?? 1.0;
                operativeTemperature = heatingSetpoint;
            }
            else if (freeFloatingTemperature > coolingSetpoint)
            {
                coolingLoad = Math.Max(
                    0,
                    baseBalance - coolingSetpoint * (thermalCapacityPerHour + totalHeatTransfer));

                coolingLoad *= preferences?.CoolingSafetyFactor ?? 1.0;
                operativeTemperature = coolingSetpoint;
            }

            var operativeGroundBoundaryReferenceTemperature = CalculateGroundBoundaryReferenceTemperature(
                groundBoundary,
                operativeTemperature,
                weather.DryBulbTemperature,
                groundTemperature);

            var transmissionW =
                outdoorUa * Math.Abs(operativeTemperature - weather.DryBulbTemperature);

            var ventilationComponentLoads = ventilationComponents.WithLoads(
                weather.DryBulbTemperature,
                operativeTemperature);

            var groundW =
                groundUa * Math.Abs(operativeTemperature - operativeGroundBoundaryReferenceTemperature);

            // Sign convention:
            // Positive signed component = heat gain to the room.
            // Negative signed component = heat loss from the room.
            var transmissionBalanceW =
                outdoorUa * (weather.DryBulbTemperature - operativeTemperature);

            var groundBalanceW =
                groundUa * (operativeGroundBoundaryReferenceTemperature - operativeTemperature);

            roomResults.Add(new Iso52016RoomHourResult(
                room.Id,
                new Iso52016RoomHourlyEnergyNeed(
                    RoomId: room.Id,
                    RoomName: room.Name,
                    ZoneName: zone.Name,
                    HourOfYear: hourOfYear,
                    Month: Iso52016HourlyCalculatorMath.GetMonth(hourOfYear),
                    HeatingLoadW: Iso52016HourlyCalculatorMath.Round(heatingLoad),
                    CoolingLoadW: Iso52016HourlyCalculatorMath.Round(coolingLoad),
                    OperativeTemperatureC: Iso52016HourlyCalculatorMath.Round(operativeTemperature),
                    OutdoorTemperatureC: Iso52016HourlyCalculatorMath.Round(weather.DryBulbTemperature),
                    InternalGainsW: Iso52016HourlyCalculatorMath.Round(internalGains),
                    SolarGainsW: Iso52016HourlyCalculatorMath.Round(solarGains),
                    TransmissionW: Iso52016HourlyCalculatorMath.Round(transmissionW),
                    VentilationW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.TotalVentilationW),
                    InfiltrationW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.InfiltrationW),
                    GroundW: Iso52016HourlyCalculatorMath.Round(groundW),
                    TransmissionBalanceW: Iso52016HourlyCalculatorMath.Round(transmissionBalanceW),
                    VentilationBalanceW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.TotalVentilationBalanceW),
                    InfiltrationBalanceW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.InfiltrationBalanceW),
                    GroundBalanceW: Iso52016HourlyCalculatorMath.Round(groundBalanceW),
                    MechanicalVentilationW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.MechanicalVentilationW),
                    NaturalVentilationW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.NaturalVentilationW),
                    MechanicalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.MechanicalVentilationBalanceW),
                    NaturalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.NaturalVentilationBalanceW))));
        }

        var zoneHeating = roomResults.Sum(x => x.Hour.HeatingLoadW);
        var zoneCooling = roomResults.Sum(x => x.Hour.CoolingLoadW);
        var zoneInternal = roomResults.Sum(x => x.Hour.InternalGainsW);
        var zoneSolar = roomResults.Sum(x => x.Hour.SolarGainsW);
        var zoneTransmission = roomResults.Sum(x => x.Hour.TransmissionW);
        var zoneVentilation = roomResults.Sum(x => x.Hour.VentilationW);
        var zoneMechanicalVentilation = roomResults.Sum(x => x.Hour.MechanicalVentilationW);
        var zoneNaturalVentilation = roomResults.Sum(x => x.Hour.NaturalVentilationW);
        var zoneInfiltration = roomResults.Sum(x => x.Hour.InfiltrationW);
        var zoneGround = roomResults.Sum(x => x.Hour.GroundW);

        var zoneTransmissionBalance = roomResults.Sum(x => x.Hour.TransmissionBalanceW);
        var zoneVentilationBalance = roomResults.Sum(x => x.Hour.VentilationBalanceW);
        var zoneMechanicalVentilationBalance = roomResults.Sum(x => x.Hour.MechanicalVentilationBalanceW);
        var zoneNaturalVentilationBalance = roomResults.Sum(x => x.Hour.NaturalVentilationBalanceW);
        var zoneInfiltrationBalance = roomResults.Sum(x => x.Hour.InfiltrationBalanceW);
        var zoneGroundBalance = roomResults.Sum(x => x.Hour.GroundBalanceW);

        var totalArea = rooms.Sum(room => room.Area.SquareMeters);
        var zoneOperative = totalArea > 0
            ? rooms.Join(
                    roomResults,
                    room => room.Id,
                    result => result.RoomId,
                    (room, result) => room.Area.SquareMeters * result.Hour.OperativeTemperatureC)
                .Sum() / totalArea
            : roomResults.Average(x => x.Hour.OperativeTemperatureC);

        return new Iso52016ZoneHourResult(
            zone.Name,
            new Iso52016HourlyEnergyNeed(
                HourOfYear: hourOfYear,
                Month: Iso52016HourlyCalculatorMath.GetMonth(hourOfYear),
                HeatingLoadW: Iso52016HourlyCalculatorMath.Round(zoneHeating),
                CoolingLoadW: Iso52016HourlyCalculatorMath.Round(zoneCooling),
                OperativeTemperatureC: Iso52016HourlyCalculatorMath.Round(zoneOperative),
                OutdoorTemperatureC: Iso52016HourlyCalculatorMath.Round(weather.DryBulbTemperature),
                InternalGainsW: Iso52016HourlyCalculatorMath.Round(zoneInternal),
                SolarGainsW: Iso52016HourlyCalculatorMath.Round(zoneSolar),
                TransmissionW: Iso52016HourlyCalculatorMath.Round(zoneTransmission),
                VentilationW: Iso52016HourlyCalculatorMath.Round(zoneVentilation),
                InfiltrationW: Iso52016HourlyCalculatorMath.Round(zoneInfiltration),
                GroundW: Iso52016HourlyCalculatorMath.Round(zoneGround),
                TransmissionBalanceW: Iso52016HourlyCalculatorMath.Round(zoneTransmissionBalance),
                VentilationBalanceW: Iso52016HourlyCalculatorMath.Round(zoneVentilationBalance),
                InfiltrationBalanceW: Iso52016HourlyCalculatorMath.Round(zoneInfiltrationBalance),
                GroundBalanceW: Iso52016HourlyCalculatorMath.Round(zoneGroundBalance),
                MechanicalVentilationW: Iso52016HourlyCalculatorMath.Round(zoneMechanicalVentilation),
                NaturalVentilationW: Iso52016HourlyCalculatorMath.Round(zoneNaturalVentilation),
                MechanicalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(zoneMechanicalVentilationBalance),
                NaturalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(zoneNaturalVentilationBalance)),
            roomResults);
    }

    private double GetHourlySolarGain(
        Room room,
        AnnualHourlyData weather,
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

    private double GetHourlyInternalGain(
        Room room,
        int hour,
        AnnualProfileOptionsDto? annualProfileOptions,
        RoomHourlyProfileSnapshot? annualSnapshot)
    {
        var useAnnualProfiles = annualProfileOptions?.UseAnnualProfiles == true && annualSnapshot is not null;

        var occupancyFactor = useAnnualProfiles
            ? annualSnapshot!.Occupancy
            : GetOccupancyFactor(room, hour);

        var equipmentFactor = useAnnualProfiles
            ? annualSnapshot!.Equipment
            : GetEquipmentFactor(room, hour);

        var lightingFactor = useAnnualProfiles
            ? annualSnapshot!.Lighting
            : GetLightingFactor(room, hour);

        var people = room.PeopleCount *
                     Iso52016HourlyCalculatorMath.GetPeopleHeatGain(room.Type) *
                     occupancyFactor;

        var equipment = room.EquipmentLoad.Watts * equipmentFactor;
        var lighting = room.LightingLoad.Watts * lightingFactor;

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

    private Iso52016HourlyVentilationComponents GetHourlyVentilationComponentsForRoom(
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
        {
            return new Iso52016HourlyVentilationComponents(
                MechanicalHeatTransferWPerK: 0,
                NaturalHeatTransferWPerK: natural,
                InfiltrationHeatTransferWPerK: 0,
                TotalVentilationHeatTransferWPerK: natural);
        }

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

        return new Iso52016HourlyVentilationComponents(
            MechanicalHeatTransferWPerK: mechanical,
            NaturalHeatTransferWPerK: natural,
            InfiltrationHeatTransferWPerK: infiltration,
            TotalVentilationHeatTransferWPerK: mechanical + natural);
    }

    private Iso52016AdjacentBoundaryContribution CalculateAdjacentBoundaryContributionForRoom(
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
            var heatTransferCoefficient = wall.Area.SquareMeters * Iso52016HourlyCalculatorMath.GetWallUValue(wall);

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

        return new Iso52016AdjacentBoundaryContribution(
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

    private static double GetOutdoorEnvelopeHeatTransferCoefficient(
        Room room,
        Models.ReferenceData.BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.External)
            .Sum(wall => wall.Area.SquareMeters * Iso52016HourlyCalculatorMath.GetWallUValue(wall));

        envelope += room.Windows.Sum(window => window.Area.SquareMeters * window.UValue.Value);
        envelope += room.Area.SquareMeters * envelopeDefaults.CeilingUValueWPerM2K;

        return envelope;
    }

    private static double CalculateGroundBoundaryReferenceTemperature(
        GroundBoundaryCondition groundBoundary,
        double indoorReferenceTemperatureC,
        double outdoorTemperatureC,
        double groundTemperatureC) =>
        groundBoundary.IndoorTemperatureWeight * indoorReferenceTemperatureC +
        groundBoundary.OutdoorTemperatureWeight * outdoorTemperatureC +
        groundBoundary.GroundTemperatureWeight * groundTemperatureC;

    private static double GetGroundEnvelopeHeatTransferCoefficient(
        Room room,
        Models.ReferenceData.BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.Ground)
            .Sum(wall => wall.Area.SquareMeters * Iso52016HourlyCalculatorMath.GetWallUValue(wall));

        envelope += room.Area.SquareMeters * envelopeDefaults.FloorUValueWPerM2K;

        return envelope;
    }
}
