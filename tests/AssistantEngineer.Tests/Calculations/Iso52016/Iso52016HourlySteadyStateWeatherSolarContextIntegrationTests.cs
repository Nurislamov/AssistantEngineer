using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016HourlySteadyStateWeatherSolarContextIntegrationTests
{
    [Fact]
    public async Task CalculateBuildingEnergyNeedsAsync_PassesWeatherSolarHoursIntoHeatBalance()
    {
        var annualClimateData = CreateAnnualClimateData();
        var building = CreateBuildingWithSouthWindow(annualClimateData.ClimateZone);

        var calculator = new Iso52016HourlySteadyStateCalculator(
            new FixedAnnualClimateDataProvider(annualClimateData),
            new ZeroSolarRadiationService(),
            options: Options.Create(new Iso52016EnergyNeedOptions
            {
                LatitudeDegrees = 41.3,
                LongitudeDegrees = 69.2,
                TimeZoneOffsetHours = 5,
                GroundReflectance = 0.2,
                DefaultSolarUtilizationFactor = 1.0,
                DefaultWindowFrameAreaFraction = 0.0,
                DefaultDirectSolarShadingReductionFactor = 1.0
            }),
            profileOptions: Options.Create(new En16798ProfileOptions
            {
                UseStandardProfilesWhenMissingSchedules = false
            }),
            weatherSolarContextBuilder: new SyntheticWeatherSolarContextBuilder());

        var result = await calculator.CalculateBuildingEnergyNeedsAsync(
            building,
            year: annualClimateData.Year,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(result.HourlyResults, hour => hour.SolarGainsW > 0);
        Assert.True(result.Breakdown.SolarGainsKWh > 0);
    }

    private static Building CreateBuildingWithSouthWindow(
        ClimateZone climateZone)
    {
        var project = Project.Create("Project").Value;
        var building = Building.Create("Building", project, climateZone).Value;
        var floor = building.AddFloor("Floor").Value;

        var room = floor.AddRoom(
            "Room",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 0,
            equipmentLoad: Power.FromWatts(0).Value,
            lightingLoad: Power.FromWatts(0).Value,
            type: RoomType.Office).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        Assert.True(room.AddWindow(
            Area.FromSquareMeters(2).Value,
            ThermalTransmittance.FromValue(1.5).Value,
            SolarHeatGainCoefficient.FromValue(0.6).Value,
            CardinalDirection.South).IsSuccess);

        return building;
    }

    private static AnnualClimateData CreateAnnualClimateData()
    {
        var climateZone = ClimateZone.Create(
            "Climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;

        var annualData = AnnualClimateData.Create(
            climateZone,
            year: 2026).Value;

        for (var hour = 0; hour < 8760; hour++)
        {
            var hourOfDay = hour % 24;
            var isDay = hourOfDay is >= 9 and <= 15;

            var addResult = annualData.AddHourlyData(
                hourOfYear: hour,
                dryBulbTemp: 10,
                directSolar: isDay ? 600 : 0,
                diffuseSolar: isDay ? 100 : 0,
                relativeHumidityPercent: 50,
                atmosphericPressurePa: 101_325,
                windSpeedMPerS: 2.5,
                windDirectionDegrees: 180,
                horizontalInfraredRadiationWPerM2: 300,
                skyTemperatureC: 0,
                totalSkyCoverTenths: 5,
                opaqueSkyCoverTenths: 4);

            Assert.True(addResult.IsSuccess);
        }

        return annualData;
    }

    private sealed class FixedAnnualClimateDataProvider : IAnnualClimateDataProvider
    {
        private readonly AnnualClimateData _annualClimateData;

        public FixedAnnualClimateDataProvider(
            AnnualClimateData annualClimateData)
        {
            _annualClimateData = annualClimateData;
        }

        public Task<AnnualClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AnnualClimateData?>(_annualClimateData);
    }

    private sealed class ZeroSolarRadiationService : ISolarRadiationService
    {
        public double CalculateVerticalSurfaceRadiation(
            AnnualHourlyData hourlyData,
            CardinalDirection orientation,
            double latitude,
            int dayOfYear,
            int hour) =>
            0.0;
    }

    private sealed class SyntheticWeatherSolarContextBuilder : IIso52016WeatherSolarContextBuilder
    {
        public Result<Iso52016WeatherSolarContext> Build(
            Iso52016WeatherSolarContextRequest request)
        {
            var hours = request
                .AnnualClimateData
                .HourlyData
                .OrderBy(hour => hour.HourOfYear)
                .Select(hour =>
                {
                    var hourOfDay = hour.HourOfYear % 24;
                    var isSolarHour = hourOfDay is >= 9 and <= 15;
                    var beam = isSolarHour ? 300.0 : 0.0;
                    var diffuse = isSolarHour ? 100.0 : 0.0;
                    var ground = isSolarHour ? 20.0 : 0.0;
                    var total = beam + diffuse + ground;

                    return new Iso52016HourlyWeatherSolarRecord(
                        HourOfYear: hour.HourOfYear,
                        Month: 6,
                        Day: 29,
                        Hour: hourOfDay,
                        OutdoorTemperatureC: hour.DryBulbTemperature,
                        GroundBoundaryTemperatureC: 12,
                        SolarAltitudeDegrees: isSolarHour ? 45 : -5,
                        SolarAzimuthDegrees: 180,
                        DirectNormalIrradianceWm2: hour.DirectSolarRadiation,
                        DiffuseHorizontalIrradianceWm2: hour.DiffuseSolarRadiation,
                        GlobalHorizontalIrradianceWm2: total,
                        SurfaceIrradiance:
                        [
                            new Iso52016SurfaceWeatherSolarRecord(
                                SurfaceCode: "south",
                                Orientation: SurfaceOrientation.SouthVertical,
                                IncidenceAngleDegrees: 30,
                                BeamIrradianceWm2: beam,
                                DiffuseSkyIrradianceWm2: diffuse,
                                GroundReflectedIrradianceWm2: ground,
                                TotalIrradianceWm2: total)
                        ]);
                })
                .ToArray();

            return Result<Iso52016WeatherSolarContext>.Success(
                new Iso52016WeatherSolarContext(
                    Year: request.AnnualClimateData.Year,
                    TimeZoneOffset: request.TimeZoneOffset,
                    LatitudeDegrees: request.LatitudeDegrees,
                    LongitudeDegrees: request.LongitudeDegrees,
                    Hours: hours));
        }
    }
}
