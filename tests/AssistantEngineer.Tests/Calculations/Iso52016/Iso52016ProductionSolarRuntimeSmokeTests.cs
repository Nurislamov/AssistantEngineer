using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016ProductionSolarRuntimeSmokeTests
{
    [Fact]
    public async Task AddCalculationsModule_ResolvedHourlySteadyStateCalculatorUsesWeatherSolarContextPath()
    {
        var annualClimateData = CreateAnnualClimateData();
        var building = CreateBuildingWithSouthWindow(annualClimateData.ClimateZone);

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        // Test-specific weather provider. Single-service resolution uses the last registration.
        services.AddSingleton<IAnnualClimateDataProvider>(
            new FixedAnnualClimateDataProvider(annualClimateData));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var calculator = scope.ServiceProvider.GetRequiredService<Iso52016HourlySteadyStateCalculator>();

        var result = await calculator.CalculateBuildingEnergyNeedsAsync(
            building,
            year: annualClimateData.Year,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Diagnostics);

        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == "Iso52016.WeatherSolarContextUsed");

        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == "Iso52016.SolarGainComponentPathUsed");

        Assert.DoesNotContain(result.Diagnostics!, diagnostic =>
            diagnostic.Code == "Iso52016.MatrixSolarRadiationFallbackUsed");

        Assert.True(
            result.Breakdown.SolarGainsKWh > 0,
            "Production DI runtime path should produce non-zero window solar gains from the ISO 52016 weather-solar context.");
    }

    private static Building CreateBuildingWithSouthWindow(
        ClimateZone climateZone)
    {
        var project = Project.Create("Production runtime smoke project").Value;
        var building = Building.Create("Production runtime smoke building", project, climateZone).Value;
        var floor = building.AddFloor("Ground").Value;

        var room = floor.AddRoom(
            "South solar room",
            Area.FromSquareMeters(30).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 0,
            equipmentLoad: Power.FromWatts(0).Value,
            lightingLoad: Power.FromWatts(0).Value,
            type: RoomType.Office).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(15).Value,
            ThermalTransmittance.FromValue(0.35).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        Assert.True(room.AddWindow(
            Area.FromSquareMeters(4).Value,
            ThermalTransmittance.FromValue(1.4).Value,
            SolarHeatGainCoefficient.FromValue(0.6).Value,
            CardinalDirection.South).IsSuccess);

        return building;
    }

    private static AnnualClimateData CreateAnnualClimateData()
    {
        var climateZone = ClimateZone.Create(
            "Production runtime smoke climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;

        var annualData = AnnualClimateData.Create(
            climateZone,
            year: 2026).Value;

        for (var hour = 0; hour < 8760; hour++)
        {
            var hourOfDay = hour % 24;
            var daylightShape = hourOfDay is >= 7 and <= 17
                ? Math.Sin(Math.PI * (hourOfDay - 6) / 12.0)
                : 0.0;

            var addResult = annualData.AddHourlyData(
                hourOfYear: hour,
                dryBulbTemp: 10,
                directSolar: 600 * daylightShape,
                diffuseSolar: 100 * daylightShape,
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
}
