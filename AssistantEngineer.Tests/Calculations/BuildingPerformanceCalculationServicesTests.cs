using AssistantEngineer.Modules.Benchmarks.Application.Services;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class BuildingPerformanceCalculationServicesTests
{
    [Fact]
    public void AnnualProfileGeneratorBuildsNonLeapHourlyProfileWithWeekendOverrides()
    {
        var generator = new AnnualProfileGenerator();
        var weekday = Enumerable.Repeat(1.0, 24).ToArray();
        var weekend = Enumerable.Repeat(0.25, 24).ToArray();

        var profile = generator.Generate(new AnnualProfileRequest(
            "Office occupancy",
            2021,
            weekday,
            weekend,
            new HashSet<DateOnly> { new(2021, 1, 4) }));

        Assert.Equal(8760, profile.Values.Count);
        Assert.Equal(1.0, profile.Values[0]);
        Assert.Equal(0.25, profile.Values[24]);
        Assert.Equal(0.25, profile.Values[72]);
    }

    [Fact]
    public void DomesticHotWaterDemandCalculatesAnnualVolumeAndEnergy()
    {
        var service = new DomesticHotWaterDemandService();

        var result = service.Calculate(new DomesticHotWaterDemandRequest
        {
            PeopleCount = 3,
            LitersPerPersonDay = 40,
            ColdWaterTemperatureC = 10,
            HotWaterTemperatureC = 60
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(120, result.Value.DailyVolumeLiters);
        Assert.Equal(12, result.Value.MonthlyDemand.Count);
        Assert.Equal(43_800, result.Value.AnnualVolumeLiters);
        Assert.True(result.Value.AnnualEnergyKWh > 2500);
        Assert.Empty(result.Value.HourlyDemand);
    }

    [Fact]
    public void DomesticHotWaterDemandCanBuildHourlyProfile()
    {
        var service = new DomesticHotWaterDemandService();

        var result = service.Calculate(new DomesticHotWaterDemandRequest
        {
            PeopleCount = 2,
            LitersPerPersonDay = 50,
            IncludeHourlyProfile = true,
            StorageLossKWhPerDay = 1.2,
            CirculationLossKWhPerDay = 0.8
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(8760, result.Value.HourlyDemand.Count);
        Assert.Equal(
            result.Value.AnnualVolumeLiters,
            result.Value.HourlyDemand.Sum(hour => hour.VolumeLiters),
            precision: 0);
        Assert.True(result.Value.HourlyDemand.Sum(hour => hour.EnergyKWh) > result.Value.AnnualEnergyKWh * 0.98);
    }

    [Fact]
    public void HeatingSystemEnergyConvertsUsefulDemandToFinalEnergy()
    {
        var service = new HeatingSystemEnergyService();
        var energyNeed = CreateEnergyNeed(annualHeatingDemandKWh: 1000);

        var result = service.Calculate(energyNeed, new HeatingSystemEnergyRequest
        {
            GenerationEfficiency = 0.9,
            DistributionEfficiency = 0.95,
            EmissionEfficiency = 0.98
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1000, result.Value.UsefulHeatingDemandKWh);
        Assert.True(result.Value.FinalHeatingEnergyKWh > result.Value.UsefulHeatingDemandKWh);
        Assert.True(result.Value.TotalSystemEfficiency < 1);
    }

    [Fact]
    public void CoolingSystemEnergyConvertsUsefulCoolingToElectricity()
    {
        var service = new CoolingSystemEnergyService();
        var energyNeed = CreateEnergyNeed(annualCoolingDemandKWh: 1200);

        var result = service.Calculate(energyNeed, new CoolingSystemEnergyRequest
        {
            SeasonalCop = 3,
            DistributionEfficiency = 0.95,
            EmissionEfficiency = 0.98,
            AuxiliaryEnergyKWh = 40
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1200, result.Value.UsefulCoolingDemandKWh);
        Assert.True(result.Value.FinalCoolingElectricityKWh < result.Value.UsefulCoolingDemandKWh);
        Assert.True(result.Value.FinalCoolingElectricityKWh > result.Value.CompressorElectricityKWh);
    }

    [Fact]
    public void BuildingEnergyPerformanceSummaryAggregatesFinalPrimaryAndCo2Energy()
    {
        var service = new BuildingEnergyPerformanceSummaryService(
            new HeatingSystemEnergyService(),
            new CoolingSystemEnergyService(),
            new DomesticHotWaterDemandService(),
            new EnergyCarrierFactorProvider());
        var building = CreatePerformanceBuilding(floorAreaM2: 100);
        var energyNeed = CreateEnergyNeed(annualHeatingDemandKWh: 1000, annualCoolingDemandKWh: 600);

        var result = service.Calculate(building, energyNeed, new BuildingEnergyPerformanceRequest
        {
            HeatingSystem = new HeatingSystemEnergyRequest
            {
                GenerationEfficiency = 0.9,
                DistributionEfficiency = 0.95,
                EmissionEfficiency = 0.98
            },
            CoolingSystem = new CoolingSystemEnergyRequest
            {
                SeasonalCop = 3,
                DistributionEfficiency = 0.95,
                EmissionEfficiency = 0.98,
                AuxiliaryEnergyKWh = 20
            },
            IncludeDomesticHotWater = true,
            DomesticHotWater = new DomesticHotWaterDemandRequest
            {
                PeopleCount = 2,
                LitersPerPersonDay = 35
            },
            DomesticHotWaterSystem = new DomesticHotWaterSystemRequest
            {
                GenerationEfficiency = 0.9
            }
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.EndUses.Count);
        Assert.True(result.Value.TotalFinalEnergyKWh > result.Value.TotalUsefulEnergyKWh);
        Assert.True(result.Value.TotalPrimaryEnergyKWh > result.Value.TotalFinalEnergyKWh);
        Assert.True(result.Value.TotalCo2Kg > 0);
        Assert.Equal(
            result.Value.TotalFinalEnergyKWh / result.Value.FloorAreaM2,
            result.Value.FinalEnergyIntensityKWhPerM2Year,
            precision: 2);
    }

    [Fact]
    public void BuildingEnergyPerformanceSummaryUsesCarrierFactorOverrides()
    {
        var service = new BuildingEnergyPerformanceSummaryService(
            new HeatingSystemEnergyService(),
            new CoolingSystemEnergyService(),
            new DomesticHotWaterDemandService(),
            new EnergyCarrierFactorProvider());
        var building = CreatePerformanceBuilding(floorAreaM2: 50);
        var energyNeed = CreateEnergyNeed(annualHeatingDemandKWh: 500);

        var result = service.Calculate(building, energyNeed, new BuildingEnergyPerformanceRequest
        {
            HeatingCarrier = EnergyCarrierType.Electricity,
            CarrierFactorOverrides = new Dictionary<EnergyCarrierType, EnergyCarrierFactors>
            {
                [EnergyCarrierType.Electricity] = new(1, 0)
            }
        });

        Assert.True(result.IsSuccess);
        var heating = Assert.Single(result.Value.EndUses, item => item.EndUse == BuildingEnergyEndUse.Heating);
        Assert.Equal(1, heating.PrimaryEnergyFactor);
        Assert.Equal(0, heating.Co2Kg);
    }

    [Fact]
    public void EnergySignatureBuildsMonthlyRegressionFromHourlyIso52016Results()
    {
        var service = new EnergySignatureService();
        var hourlyResults = Enumerable.Range(1, 12)
            .Select(month => new Iso52016HourlyEnergyNeed(
                HourOfYear: (month - 1) * 730,
                Month: month,
                HeatingLoadW: (18 - month) * 100,
                CoolingLoadW: 0,
                OperativeTemperatureC: 20,
                OutdoorTemperatureC: month,
                InternalGainsW: 0,
                SolarGainsW: 0))
            .ToArray();
        var energyNeed = CreateEnergyNeed(hourlyResults: hourlyResults);

        var result = service.Calculate(energyNeed);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value.Points.Count);
        Assert.True(result.Value.SlopeKWhPerHdd > 0);
        Assert.InRange(result.Value.RSquared, 0, 1);
    }

    [Fact]
    public void VentilationCalculatorUsesIso16798OccupancyDefaults()
    {
        var room = DomainInvariantTests.CreateRoom(areaM2: 20);
        var calculator = new VentilationHeatTransferCalculator(new Iso16798ReferenceData());

        var fixedAirChanges = calculator.Calculate(
            room,
            new VentilationCalculationContext(
                VentilationCalculationMethod.FixedAirChanges,
                IndoorTemperatureC: 21,
                OutdoorTemperatureC: 0));
        var occupancyVentilation = calculator.Calculate(
            room,
            new VentilationCalculationContext(
                VentilationCalculationMethod.Occupancy,
                IndoorTemperatureC: 21,
                OutdoorTemperatureC: 0));

        Assert.True(fixedAirChanges > 0);
        Assert.True(occupancyVentilation > fixedAirChanges);
    }

    [Fact]
    public void VentilationCalculatorAddsWindDrivenInfiltrationWithoutHeatRecovery()
    {
        var room = DomainInvariantTests.CreateRoom(areaM2: 20);
        var ventilation = VentilationParameters.Create(
            airChangesPerHour: 1,
            heatRecoveryEfficiency: 1,
            infiltrationAirChangesPerHour: 0.1,
            windExposureFactor: 1.2,
            stackCoefficient: 0.01,
            windCoefficient: 0.03).Value;
        Assert.True(room.SetVentilationParameters(ventilation).IsSuccess);
        var calculator = new VentilationHeatTransferCalculator(new Iso16798ReferenceData());

        var calm = calculator.Calculate(
            room,
            new VentilationCalculationContext(
                VentilationCalculationMethod.TemperatureWind,
                IndoorTemperatureC: 21,
                OutdoorTemperatureC: 0,
                WindSpeedMPerS: 0));
        var windy = calculator.Calculate(
            room,
            new VentilationCalculationContext(
                VentilationCalculationMethod.TemperatureWind,
                IndoorTemperatureC: 21,
                OutdoorTemperatureC: 0,
                WindSpeedMPerS: 6));

        Assert.True(calm > 0);
        Assert.True(windy > calm);
    }

    [Fact]
    public void WindowShadingServiceReducesSouthSolarAtHighSun()
    {
        var service = new WindowShadingService();

        var reduction = service.CalculateCombinedSolarReduction(
            CardinalDirection.South,
            latitudeDegrees: 41,
            dayOfYear: 172,
            hourOfDay: 12,
            new WindowShadingOptions(
                OverhangDepthM: 1,
                SideFinDepthM: 0,
                RevealDepthM: 0,
                WindowHeightM: 1.5,
                WindowWidthM: 1.5,
                MinimumDirectSolarReductionFactor: 0.15,
                DiffuseSolarShareUnaffected: 0.3));

        Assert.InRange(reduction, 0.15, 0.95);
    }

    [Fact]
    public async Task Iso52016ReferenceBenchmarksPassCorePhysicalAssertions()
    {
        var service = new Iso52016ReferenceBenchmarkService(
            new SolarRadiationService(),
            new VentilationHeatTransferCalculator(new Iso16798ReferenceData()),
            new WindowShadingService(),
            Options.Create(new Iso52016EnergyNeedOptions()));

        var results = await service.RunAsync();

        Assert.Equal(3, results.Count);
        Assert.All(results, result => Assert.True(result.Passed, FormatBenchmarkFailure(result)));
    }

    [Fact]
    public async Task Iso52016UsesPerWindowShadingParameters()
    {
        var climateZone = ClimateZone.Create(
            "Per-window shading climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(0).Value).Value;
        var weather = AnnualClimateData.Create(climateZone, year: 2020).Value;
        for (var hour = 0; hour < 8760; hour++)
        {
            var hourOfDay = hour % 24;
            var daylight = hourOfDay is >= 7 and <= 17
                ? Math.Sin(Math.PI * (hourOfDay - 6) / 12.0)
                : 0;
            Assert.True(weather.AddHourlyData(
                hour,
                dryBulbTemp: 22,
                directSolar: 550 * daylight,
                diffuseSolar: 90 * daylight).IsSuccess);
        }

        var unshaded = await CalculateSolarCaseAsync(climateZone, weather, WindowShadingParameters.None);
        var shaded = await CalculateSolarCaseAsync(
            climateZone,
            weather,
            WindowShadingParameters.Create(
                overhangDepthM: 1.2,
                sideFinDepthM: 0.4,
                revealDepthM: 0.1,
                windowHeightM: 1.5,
                windowWidthM: 1.8,
                minimumDirectSolarReductionFactor: 0.15,
                diffuseSolarShareUnaffected: 0.3).Value);

        Assert.True(unshaded.Breakdown.SolarGainsKWh > shaded.Breakdown.SolarGainsKWh);
        Assert.True(shaded.Breakdown.SolarGainsKWh > 0);
    }

    [Fact]
    public async Task Iso52016UsesProjectCalculationPreferencesForSolarCalibration()
    {
        var climateZone = ClimateZone.Create(
            "Preference solar climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(0).Value).Value;
        var weather = AnnualClimateData.Create(climateZone, year: 2020).Value;
        for (var hour = 0; hour < 8760; hour++)
        {
            var hourOfDay = hour % 24;
            var daylight = hourOfDay is >= 7 and <= 17
                ? Math.Sin(Math.PI * (hourOfDay - 6) / 12.0)
                : 0;
            Assert.True(weather.AddHourlyData(
                hour,
                dryBulbTemp: 22,
                directSolar: 550 * daylight,
                diffuseSolar: 90 * daylight).IsSuccess);
        }
        var noSolarPreferences = CalculationPreferences.Create(
            coolingSafetyFactor: 1,
            heatingSafetyFactor: 1,
            iso52016SolarUtilizationFactor: 0).Value;

        var defaultResult = await CalculateSolarCaseAsync(climateZone, weather, WindowShadingParameters.None);
        var calibratedResult = await CalculateSolarCaseAsync(
            climateZone,
            weather,
            WindowShadingParameters.None,
            noSolarPreferences);

        Assert.True(defaultResult.Breakdown.SolarGainsKWh > 0);
        Assert.Equal(0, calibratedResult.Breakdown.SolarGainsKWh);
    }

    private static string FormatBenchmarkFailure(Iso52016ReferenceBenchmarkResult result)
    {
        var failed = result.Assertions
            .Where(assertion => !assertion.Passed)
            .Select(assertion =>
                $"{assertion.Name}: actual={assertion.Actual}, expected={assertion.Expected}, tolerance={assertion.Tolerance}")
            .ToArray();
        return $"{result.CaseId} failed. Metrics: {string.Join(", ", result.Metrics.Select(metric => $"{metric.Key}={metric.Value}"))}. Failed assertions: {string.Join("; ", failed)}";
    }

    private static Iso52016AnnualEnergyNeedResult CreateEnergyNeed(
        double annualHeatingDemandKWh = 0,
        double annualCoolingDemandKWh = 0,
        IReadOnlyList<Iso52016HourlyEnergyNeed>? hourlyResults = null) =>
        new(
            BuildingId: 1,
            BuildingName: "Building",
            Year: 2021,
            HourlyResults: hourlyResults ?? [],
            MonthlyResults: [],
            AnnualHeatingDemandKWh: annualHeatingDemandKWh,
            AnnualCoolingDemandKWh: annualCoolingDemandKWh,
            Breakdown: new Iso52016EnergyBalanceBreakdown(
                SolarGainsKWh: 0,
                InternalGainsKWh: 0,
                HeatingInputKWh: annualHeatingDemandKWh,
                CoolingExtractedKWh: annualCoolingDemandKWh));

    private static Building CreatePerformanceBuilding(double floorAreaM2)
    {
        var project = DomainInvariantTests.CreateProject("Performance project");
        var building = Building.Create("Performance building", project).Value;
        var floor = building.AddFloor("Ground").Value;
        _ = floor.AddRoom(
            "Performance room",
            Area.FromSquareMeters(floorAreaM2).Value,
            3,
            Temperature.FromCelsius(21).Value,
            Temperature.FromCelsius(35).Value).Value;
        return building;
    }

    private static async Task<Iso52016AnnualEnergyNeedResult> CalculateSolarCaseAsync(
        ClimateZone climateZone,
        AnnualClimateData weather,
        WindowShadingParameters shading,
        CalculationPreferences? preferences = null)
    {
        var project = DomainInvariantTests.CreateProject("Solar case project");
        var building = Building.Create("Solar case building", project, climateZone).Value;
        var floor = building.AddFloor("Ground").Value;
        var room = floor.AddRoom(
            "Solar room",
            Area.FromSquareMeters(50).Value,
            3,
            Temperature.FromCelsius(21).Value,
            Temperature.FromCelsius(35).Value).Value;
        Assert.True(room.AddWall(
            Area.FromSquareMeters(30).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(0.35).Value,
            CardinalDirection.South).IsSuccess);
        Assert.True(room.AddWindow(
            Area.FromSquareMeters(8).Value,
            ThermalTransmittance.FromValue(1.2).Value,
            SolarHeatGainCoefficient.FromValue(0.55).Value,
            CardinalDirection.South,
            shading).IsSuccess);
        var calculator = new Iso52016HourlySteadyStateCalculator(
            new AnnualClimateDataProviderStub(weather),
            new SolarRadiationService(),
            new VentilationHeatTransferCalculator(new Iso16798ReferenceData()),
            new WindowShadingService());

        var result = await calculator.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year: weather.Year);
        Assert.NotNull(result);
        return result;
    }

    private sealed class AnnualClimateDataProviderStub : IAnnualClimateDataProvider
    {
        private readonly AnnualClimateData _weather;

        public AnnualClimateDataProviderStub(AnnualClimateData weather)
        {
            _weather = weather;
        }

        public Task<AnnualClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AnnualClimateData?>(
                climateZoneId == _weather.ClimateZoneId && year == _weather.Year
                    ? _weather
                    : null);
    }
}
