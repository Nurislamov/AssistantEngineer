using System.Text.Json;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixApplicationBaselineFixtureTests
{
    private readonly Iso52016BuildingSimulationFacade _facade =
        new(
            new Iso52016WeatherSolarContextBuilder(
                new AnnualClimateDataNormalizer(),
                new AnnualWeatherSolarProfileBuilder(
                    new SolarPositionCalculator(),
                    new IsotropicSkySurfaceIrradianceCalculator()),
                new PeriodicIso52016GroundBoundaryTemperatureProvider()),
            new Iso52016RoomEnergySimulationRequestBuilder(
                new Iso52016RoomWindowSolarGainInputMapper(),
                new Iso52016RoomEnvelopeInputCalculator(),
                new Iso52016ScheduleProfileExpander()),
            new Iso52016RoomEnergySimulationService(
                new Iso52016RoomSolarGainProfileBuilder(
                    new Iso52016WindowSolarGainCalculator()),
                new Iso52016RoomInternalGainProfileBuilder(),
                new Iso52016RoomHourlyInputProfileBuilder(),
                new Iso52016MatrixReducedRoomModelBuilder(),
                new Iso52016MatrixHourlySolver(),
                new Iso52016MatrixRoomEnergySimulationResultMapper()));

    [Theory]
    [MemberData(nameof(ApplicationBaselineFixtureFiles))]
    public void BuildingFacade_MatchesMatrixApplicationBaselineExpectations(
        string fixturePath)
    {
        var fixture = LoadFixture(fixturePath);
        var rooms = fixture.Rooms
            .Select(CreateRoomWithEnvelope)
            .ToArray();

        var result = _facade.Simulate(
            new Iso52016BuildingSimulationFacadeRequest(
                BuildingCode: fixture.BuildingCode,
                Rooms: rooms,
                AnnualClimateData: CreateAnnualClimateData(fixture),
                LatitudeDegrees: fixture.LatitudeDegrees,
                LongitudeDegrees: fixture.LongitudeDegrees,
                TimeZoneOffset: TimeSpan.FromHours(fixture.TimeZoneOffsetHours),
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: fixture.InitialIndoorTemperatureC)));

        Assert.True(result.IsSuccess, result.Error);

        var simulation = result.Value;

        Assert.Equal(fixture.BuildingCode, simulation.BuildingCode);
        Assert.Equal(fixture.Expected.RoomCount, simulation.RoomCount);
        Assert.Equal(fixture.Expected.HourCount, simulation.HourCount);
        Assert.Equal(fixture.Expected.MonthlySummaryCount, simulation.MonthlySummaries.Count);
        Assert.Equal(fixture.Expected.HourCount, simulation.Hours.Count);

        AssertInRange(
            simulation.AnnualHeatingEnergyKWh,
            fixture.Expected.AnnualHeatingEnergyKWh);

        AssertInRange(
            simulation.AnnualCoolingEnergyKWh,
            fixture.Expected.AnnualCoolingEnergyKWh);

        AssertInRange(
            simulation.PeakHeatingLoadW,
            fixture.Expected.PeakHeatingLoadW);

        AssertInRange(
            simulation.PeakCoolingLoadW,
            fixture.Expected.PeakCoolingLoadW);

        Assert.Equal(
            simulation.RoomResults.Sum(room => room.AnnualHeatingEnergyKWh),
            simulation.AnnualHeatingEnergyKWh,
            precision: 6);

        Assert.Equal(
            simulation.RoomResults.Sum(room => room.AnnualCoolingEnergyKWh),
            simulation.AnnualCoolingEnergyKWh,
            precision: 6);
    }

    [Fact]
    public void ApplicationBaselineFixtureSet_CoversColdHeatingAndHotCoolingScenarios()
    {
        var fixtureNames = ApplicationBaselineFixtureFiles()
            .Select(data => Path.GetFileNameWithoutExtension((string)data[0]))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Contains("building-cold-two-room-heating", fixtureNames);
        Assert.Contains("building-hot-single-room-cooling", fixtureNames);
    }

    public static IEnumerable<object[]> ApplicationBaselineFixtureFiles()
    {
        var baselineDirectory = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "ApplicationBaselines");

        Assert.True(
            Directory.Exists(baselineDirectory),
            $"ISO52016 Matrix application baseline directory was not found: {baselineDirectory}");

        return Directory
            .GetFiles(baselineDirectory, "*.json")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(file => new object[] { file });
    }

    private static Room CreateRoomWithEnvelope(
        ApplicationBaselineRoom roomInput)
    {
        var project = Project.Create("Matrix application baseline project").Value;
        var building = Building.Create("Matrix baseline building", project).Value;
        var floor = Floor.Create("Matrix baseline floor", building).Value;

        var room = Room.Create(
            name: roomInput.Name,
            area: Area.FromSquareMeters(roomInput.AreaM2).Value,
            heightM: 3,
            indoorTemp: Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: null,
            floor: floor,
            peopleCount: roomInput.PeopleCount,
            equipmentLoad: Power.FromWatts(roomInput.EquipmentLoadW).Value,
            lightingLoad: Power.FromWatts(roomInput.LightingLoadW).Value,
            type: RoomType.Office).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(roomInput.WallAreaM2).Value,
            ThermalTransmittance.FromValue(roomInput.WallUValueWPerM2K).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        Assert.True(room.AddWindow(
            Area.FromSquareMeters(roomInput.WindowAreaM2).Value,
            ThermalTransmittance.FromValue(1.5).Value,
            SolarHeatGainCoefficient.FromValue(0.6).Value,
            CardinalDirection.South).IsSuccess);

        return room;
    }

    private static AnnualClimateData CreateAnnualClimateData(
        ApplicationBaselineFixture fixture)
    {
        var climateZone = CreateClimateZone();

        var annualDataResult = AnnualClimateData.Create(
            climateZone,
            fixture.Year);

        Assert.True(annualDataResult.IsSuccess);

        var annualData = annualDataResult.Value;

        for (var hour = 0; hour < 8760; hour++)
        {
            var hourOfDay = hour % 24;
            var isDay = hourOfDay is >= 7 and <= 17;

            var addResult = annualData.AddHourlyData(
                hourOfYear: hour,
                dryBulbTemp: fixture.OutdoorTemperatureC,
                directSolar: isDay ? fixture.DirectSolarWm2Day : 0,
                diffuseSolar: isDay ? fixture.DiffuseSolarWm2Day : 0,
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

    private static ClimateZone CreateClimateZone()
    {
        var climateZone = ClimateZone.Create(
            "Matrix application baseline climate zone",
            Temperature.FromCelsius(38).Value,
            Temperature.FromCelsius(-30).Value);

        Assert.True(climateZone.IsSuccess);

        return climateZone.Value;
    }

    private static ApplicationBaselineFixture LoadFixture(
        string fixturePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var fixture = JsonSerializer.Deserialize<ApplicationBaselineFixture>(
            File.ReadAllText(fixturePath),
            options);

        Assert.NotNull(fixture);
        return fixture;
    }

    private static void AssertInRange(
        double actual,
        BaselineRange expectedRange) =>
        Assert.InRange(
            actual,
            expectedRange.Min,
            expectedRange.Max);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(
                directory.FullName,
                "src",
                "Backend",
                "AssistantEngineer.Modules.Calculations");

            var tests = Path.Combine(
                directory.FullName,
                "tests",
                "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate AssistantEngineer repository root from test base directory.");
    }

    private sealed record ApplicationBaselineFixture(
        string ScenarioName,
        string Description,
        string BuildingCode,
        int Year,
        double OutdoorTemperatureC,
        double DirectSolarWm2Day,
        double DiffuseSolarWm2Day,
        double LatitudeDegrees,
        double LongitudeDegrees,
        double TimeZoneOffsetHours,
        double InitialIndoorTemperatureC,
        IReadOnlyList<ApplicationBaselineRoom> Rooms,
        ApplicationBaselineExpected Expected);

    private sealed record ApplicationBaselineRoom(
        string Name,
        double AreaM2,
        int PeopleCount,
        double EquipmentLoadW,
        double LightingLoadW,
        double WallAreaM2,
        double WallUValueWPerM2K,
        double WindowAreaM2);

    private sealed record ApplicationBaselineExpected(
        int HourCount,
        int RoomCount,
        BaselineRange AnnualHeatingEnergyKWh,
        BaselineRange AnnualCoolingEnergyKWh,
        BaselineRange PeakHeatingLoadW,
        BaselineRange PeakCoolingLoadW,
        int MonthlySummaryCount);

    private sealed record BaselineRange(
        double Min,
        double Max);
}