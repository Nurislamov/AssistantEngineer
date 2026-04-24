using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Options;
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
        ILogger<Iso52016HourlySteadyStateCalculator>? logger = null)
    {
        var resolvedOptions = options?.Value ?? new Iso52016EnergyNeedOptions();
        var resolvedProfileOptions = profileOptions?.Value ?? new En16798ProfileOptions();
        var resolvedEnvelopeReferenceData = envelopeReferenceData ?? new BuildingEnvelopeReferenceData();
        var resolvedProfileCatalog = profileCatalog ?? new En16798ProfileCatalog();

        _weatherProvider = new Iso52016HourlyWeatherProvider(
            climateDataProvider,
            resolvedOptions,
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
            naturalVentilationAirflowService,
            resolvedOptions,
            resolvedProfileOptions);
        _resultComposer = new Iso52016HourlyResultComposer();
    }

    public async Task<Iso52016AnnualEnergyNeedResult?> CalculateBuildingEnergyNeedsAsync(
        Building building,
        CalculationPreferences? preferences = null,
        int? year = null,
        CancellationToken cancellationToken = default)
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
                    cancellationToken));
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
                    SolarGainsW: zoneResult.Hour.SolarGainsW));

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
        CancellationToken cancellationToken = default)
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

            var zoneResult = _heatBalanceCalculator.CalculateZoneHourEnergyNeed(
                calculationContext.Zone,
                calculationContext.ZoneState,
                weather,
                calculationContext.PreviousRoomTemperatures,
                calculationContext.RoomZoneMap,
                preferences: null,
                cancellationToken);

            foreach (var roomResult in zoneResult.Rooms)
                calculationContext.PreviousRoomTemperatures[roomResult.RoomId] =
                    roomResult.Hour.OperativeTemperatureC;

            result.Add(zoneResult.Hour.CoolingLoadW);
        }

        return result;
    }
}
