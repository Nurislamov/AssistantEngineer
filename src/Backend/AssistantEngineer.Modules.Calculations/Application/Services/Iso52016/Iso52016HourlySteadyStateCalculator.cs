using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analysis;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016HourlySteadyStateCalculator
{
    private readonly Iso52016HourlyWeatherProvider _weatherProvider;
    private readonly Iso52016HourlyCalculationContextFactory _contextFactory;
    private readonly Iso52016HourlyHeatBalanceCalculator _heatBalanceCalculator;
    private readonly Iso52016HourlyResultComposer _resultComposer;

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
        HourlyInternalGainProfileService? hourlyProfiles = null,
        IGroundTemperatureService? groundTemperatureService = null,
        IGroundHeatTransferService? groundHeatTransferService = null,
        ILogger<Iso52016HourlySteadyStateCalculator>? logger = null,
        IIso52016WeatherSolarContextBuilder? weatherSolarContextBuilder = null)
    {
        var resolvedOptions = options?.Value ?? new Iso52016EnergyNeedOptions();
        var resolvedProfileOptions = profileOptions?.Value ?? new En16798ProfileOptions();
        var resolvedEnvelopeReferenceData = envelopeReferenceData ?? new BuildingEnvelopeReferenceData();
        var resolvedProfileCatalog = profileCatalog ?? new En16798ProfileCatalog();
        var resolvedHourlyProfiles = hourlyProfiles ?? new HourlyInternalGainProfileService(
            new HourlyRoomProfileAccessor(
                new RoomAnnualProfileSetProvider(
                    new AnnualScheduleGenerationService(
                        new UzbekistanHolidayCalendarProvider(),
                        new AnnualProfileTemplateProvider()))));

        var resolvedGroundTemperatureService = groundTemperatureService ??
                                               new Iso13370GroundTemperatureService(
                                                   Microsoft.Extensions.Options.Options.Create(
                                                       new Iso13370GroundTemperatureOptions()));
        
        var resolvedGroundHeatTransferService = groundHeatTransferService ??
                                                new Iso13370GroundHeatTransferService(
                                                    Microsoft.Extensions.Options.Options.Create(
                                                        new Iso13370GroundHeatTransferOptions()));

        _weatherProvider = new Iso52016HourlyWeatherProvider(
            climateDataProvider,
            resolvedGroundTemperatureService,
            resolvedOptions,
            weatherSolarContextBuilder,
            logger);

        _contextFactory = new Iso52016HourlyCalculationContextFactory(
            resolvedEnvelopeReferenceData,
            resolvedOptions);

        _heatBalanceCalculator = new Iso52016HourlyHeatBalanceCalculator(
            solarRadiationService,
            ventilationCalculator,
            windowShadingService,
            resolvedEnvelopeReferenceData,
            resolvedProfileCatalog,
            resolvedGroundHeatTransferService,
            naturalVentilationAirflowService,
            resolvedOptions,
            resolvedProfileOptions,
            resolvedHourlyProfiles);

        _resultComposer = new Iso52016HourlyResultComposer();
    }

    public async Task<Iso52016AnnualEnergyNeedResult?> CalculateBuildingEnergyNeedsAsync(
        Building building,
        CalculationPreferences? preferences = null,
        int? year = null,
        CancellationToken cancellationToken = default,
        AnnualProfileOptionsDto? annualProfileOptions = null)
    {
        var weatherContext = await _weatherProvider.GetBuildingWeatherAsync(
            building,
            year,
            cancellationToken);

        if (weatherContext is null)
            return null;

        var calculationContext = _contextFactory.CreateBuildingContext(building, preferences);
        var zoneHourlyResults = new List<Iso52016ZoneHourlyEnergyNeed>(
            weatherContext.HourlyData.Length * Math.Max(calculationContext.Zones.Count, 1));
        var roomCount = building.Floors.SelectMany(floor => floor.Rooms).Count();
        var roomHourlyResults = new List<Iso52016RoomHourlyEnergyNeed>(
            weatherContext.HourlyData.Length * Math.Max(roomCount, 1));

        foreach (var weather in weatherContext.HourlyData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentHourResults = new List<Iso52016ZoneHourResult>(calculationContext.Zones.Count);

            foreach (var zone in calculationContext.Zones)
            {
                currentHourResults.Add(_heatBalanceCalculator.CalculateZoneHourEnergyNeed(
                    zone,
                    calculationContext.ZoneStates[zone.Name],
                    weather,
                    calculationContext.PreviousRoomTemperatures,
                    calculationContext.RoomZoneMap,
                    preferences,
                    cancellationToken,
                    annualProfileOptions,
                    weatherContext.GroundBoundaryTemperaturesC[
                        Math.Clamp(weather.HourOfYear, 0, weatherContext.GroundBoundaryTemperaturesC.Length - 1)],
                    weatherSolarHour: weatherContext.WeatherSolarContext?.GetHour(weather.HourOfYear)));
            }

            foreach (var zoneResult in currentHourResults)
            {
                zoneHourlyResults.Add(new Iso52016ZoneHourlyEnergyNeed(
                    ZoneName: zoneResult.ZoneName,
                    HourOfYear: zoneResult.Hour.HourOfYear,
                    Month: zoneResult.Hour.Month,
                    HeatingLoadW: zoneResult.Hour.HeatingLoadW,
                    CoolingLoadW: zoneResult.Hour.CoolingLoadW,
                    OperativeTemperatureC: zoneResult.Hour.OperativeTemperatureC,
                    OutdoorTemperatureC: zoneResult.Hour.OutdoorTemperatureC,
                    InternalGainsW: zoneResult.Hour.InternalGainsW,
                    SolarGainsW: zoneResult.Hour.SolarGainsW,
                    TransmissionW: zoneResult.Hour.TransmissionW,
                    VentilationW: zoneResult.Hour.VentilationW,
                    InfiltrationW: zoneResult.Hour.InfiltrationW,
                    GroundW: zoneResult.Hour.GroundW,
                    TransmissionBalanceW: zoneResult.Hour.TransmissionBalanceW,
                    VentilationBalanceW: zoneResult.Hour.VentilationBalanceW,
                    InfiltrationBalanceW: zoneResult.Hour.InfiltrationBalanceW,
                    GroundBalanceW: zoneResult.Hour.GroundBalanceW,
                    MechanicalVentilationW: zoneResult.Hour.MechanicalVentilationW,
                    NaturalVentilationW: zoneResult.Hour.NaturalVentilationW,
                    MechanicalVentilationBalanceW: zoneResult.Hour.MechanicalVentilationBalanceW,
                    NaturalVentilationBalanceW: zoneResult.Hour.NaturalVentilationBalanceW));

                foreach (var roomResult in zoneResult.Rooms)
                {
                    calculationContext.PreviousRoomTemperatures[roomResult.RoomId] =
                        roomResult.Hour.OperativeTemperatureC;
                    roomHourlyResults.Add(roomResult.Hour);
                }
            }
        }

        return _resultComposer.ComposeAnnualResult(
            building,
            weatherContext.Year,
            zoneHourlyResults,
            roomHourlyResults);
    }

    public async Task<List<double>> CalculateHourlyCoolingLoadsAsync(
        ThermalZone thermalZone,
        int year = 2020,
        double coolingSetpoint = 26.0,
        CancellationToken cancellationToken = default,
        AnnualProfileOptionsDto? annualProfileOptions = null)
    {
        var weatherContext = await _weatherProvider.GetBuildingWeatherAsync(
            thermalZone.Building,
            year,
            cancellationToken);

        if (weatherContext is null)
            return Enumerable.Repeat(0.0, 8760).ToList();

        var calculationContext = _contextFactory.CreateZoneCoolingContext(thermalZone, coolingSetpoint);
        var result = new List<double>(capacity: weatherContext.HourlyData.Length);

        foreach (var weather in weatherContext.HourlyData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groundBoundaryTemperatureC = weatherContext.GroundBoundaryTemperaturesC[
                Math.Clamp(weather.HourOfYear, 0, weatherContext.GroundBoundaryTemperaturesC.Length - 1)];

            var zoneResult = _heatBalanceCalculator.CalculateZoneHourEnergyNeed(
                calculationContext.Zone,
                calculationContext.ZoneState,
                weather,
                calculationContext.PreviousRoomTemperatures,
                calculationContext.RoomZoneMap,
                preferences: null,
                cancellationToken,
                annualProfileOptions,
                groundBoundaryTemperatureC,
                weatherSolarHour: weatherContext.WeatherSolarContext?.GetHour(weather.HourOfYear));

            foreach (var roomResult in zoneResult.Rooms)
            {
                calculationContext.PreviousRoomTemperatures[roomResult.RoomId] =
                    roomResult.Hour.OperativeTemperatureC;
            }

            result.Add(zoneResult.Hour.CoolingLoadW);
        }

        return result;
    }
}

