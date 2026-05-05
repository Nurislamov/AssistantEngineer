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

public sealed class Iso52016MatrixApplicationIntegrationHardeningTests
{
    private const double Tolerance = 0.000001;

    private readonly Iso52016BuildingSimulationFacade _facade = CreateFacade();

    [Fact]
    public void BuildingFacade_AggregatesRoomHourlyMonthlyAndAnnualResults()
    {
        var rooms = new[]
        {
            CreateRoomWithExternalWall(
                "Integration Office A",
                areaM2: 24.0,
                wallAreaM2: 18.0,
                wallUValueWPerM2K: 0.55,
                peopleCount: 2,
                equipmentLoadW: 140.0,
                lightingLoadW: 80.0),
            CreateRoomWithExternalWall(
                "Integration Office B",
                areaM2: 18.0,
                wallAreaM2: 14.0,
                wallUValueWPerM2K: 0.65,
                peopleCount: 1,
                equipmentLoadW: 90.0,
                lightingLoadW: 55.0)
        };

        var result = _facade.Simulate(
            CreateFacadeRequest(
                buildingCode: " APP-INTEGRATION-001 ",
                rooms: rooms,
                outdoorTemperatureC: -8.0,
                initialIndoorTemperatureC: 20.0));

        Assert.True(result.IsSuccess, result.Error);

        var simulation = result.Value;

        Assert.Equal("APP-INTEGRATION-001", simulation.BuildingCode);
        Assert.Equal(2, simulation.RoomCount);
        Assert.Equal(8760, simulation.HourCount);
        Assert.Equal(12, simulation.MonthlySummaries.Count);

        AssertClose(
            simulation.RoomResults.Sum(room => room.AnnualHeatingEnergyKWh),
            simulation.AnnualHeatingEnergyKWh);

        AssertClose(
            simulation.RoomResults.Sum(room => room.AnnualCoolingEnergyKWh),
            simulation.AnnualCoolingEnergyKWh);

        AssertClose(
            simulation.Hours.Sum(hour => hour.HeatingEnergyKWh),
            simulation.AnnualHeatingEnergyKWh);

        AssertClose(
            simulation.Hours.Sum(hour => hour.CoolingEnergyKWh),
            simulation.AnnualCoolingEnergyKWh);

        AssertClose(
            simulation.MonthlySummaries.Sum(month => month.HeatingEnergyKWh),
            simulation.AnnualHeatingEnergyKWh);

        AssertClose(
            simulation.MonthlySummaries.Sum(month => month.CoolingEnergyKWh),
            simulation.AnnualCoolingEnergyKWh);

        AssertClose(
            simulation.Hours.Max(hour => hour.HeatingLoadW),
            simulation.PeakHeatingLoadW);

        AssertClose(
            simulation.Hours.Max(hour => hour.CoolingLoadW),
            simulation.PeakCoolingLoadW);
    }

    [Fact]
    public void BuildingFacade_RejectsDuplicateRoomNamesBeforeSimulation()
    {
        var rooms = new[]
        {
            CreateRoomWithExternalWall("Duplicate Room", 20.0, 16.0, 0.6),
            CreateRoomWithExternalWall(" duplicate room ", 18.0, 12.0, 0.6)
        };

        var result = _facade.Simulate(
            CreateFacadeRequest(
                buildingCode: "APP-INTEGRATION-DUPLICATE",
                rooms: rooms,
                outdoorTemperatureC: 5.0,
                initialIndoorTemperatureC: 20.0));

        Assert.True(result.IsFailure);
        Assert.Contains(
            "Room names must be unique inside building simulation request",
            result.Error,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnvelopeInputCalculator_IncludesAdjacentUnconditionedAndExcludesConditionedAndAdiabaticBoundaries()
    {
        var room = CreateRoomShell("Adjacent Unconditioned Mapping", areaM2: 30.0);

        // Adjacent wall boundary types are valid domain objects only when an adjacent room is supplied.
        // This test is about the calculation mapping after domain validation succeeds.
        // Contract literal: Adjacent unconditioned domain wall requires an adjacent room object.
        var adjacentUnconditionedRoom = CreateRoomShell(
            "Adjacent Unconditioned Reference Room",
            areaM2: 12.0);

        var adjacentConditionedRoom = CreateRoomShell(
            "Adjacent Conditioned Reference Room",
            areaM2: 12.0);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(10.0).Value,
            ThermalTransmittance.FromValue(0.50).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(8.0).Value,
            ThermalTransmittance.FromValue(0.70).Value,
            CardinalDirection.North,
            WallBoundaryType.AdjacentUnconditioned,
            adjacentUnconditionedRoom).IsSuccess);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(9.0).Value,
            ThermalTransmittance.FromValue(0.90).Value,
            CardinalDirection.East,
            WallBoundaryType.AdjacentConditioned,
            adjacentConditionedRoom).IsSuccess);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(7.0).Value,
            ThermalTransmittance.FromValue(1.10).Value,
            CardinalDirection.West,
            WallBoundaryType.Adiabatic).IsSuccess);

        var calculator = new Iso52016RoomEnvelopeInputCalculator();

        var result = calculator.Calculate(
            room,
            new Iso52016RoomSimulationDefaults(
                DefaultAirChangesPerHour: 0.0,
                DefaultHeatRecoveryEfficiency: 0.0));

        Assert.True(result.IsSuccess, result.Error);

        var envelope = result.Value;
        var expectedTransmissionWPerK = 10.0 * 0.50 + 8.0 * 0.70;

        AssertClose(
            expectedTransmissionWPerK,
            envelope.TransmissionHeatTransferCoefficientWPerK);

        AssertClose(0.0, envelope.VentilationHeatTransferCoefficientWPerK);
        Assert.True(envelope.ThermalCapacityJPerK > 0.0);
    }

    [Fact]
    public void BuildingSimulationResult_GetHourPreservesHourOfYearIndexAndGuardsRange()
    {
        var result = _facade.Simulate(
            CreateFacadeRequest(
                buildingCode: "APP-INTEGRATION-HOUR-IDX",
                rooms:
                [
                    CreateRoomWithExternalWall("Indexed Room", 20.0, 15.0, 0.60)
                ],
                outdoorTemperatureC: 2.0,
                initialIndoorTemperatureC: 20.0));

        Assert.True(result.IsSuccess, result.Error);

        var simulation = result.Value;

        Assert.Equal(0, simulation.GetHour(0).HourOfYear);
        Assert.Equal(8759, simulation.GetHour(8759).HourOfYear);
        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.GetHour(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.GetHour(8760));
    }

    [Fact]
    public void BuildingFacade_ReportPathReconcilesAnnualMonthlyAndHourlyGainTotals()
    {
        var result = _facade.Simulate(
            CreateFacadeRequest(
                buildingCode: "APP-INTEGRATION-REPORT",
                rooms:
                [
                    CreateRoomWithExternalWall(
                        "Report Room A",
                        areaM2: 22.0,
                        wallAreaM2: 17.0,
                        wallUValueWPerM2K: 0.58,
                        peopleCount: 1,
                        equipmentLoadW: 100.0,
                        lightingLoadW: 65.0),
                    CreateRoomWithExternalWall(
                        "Report Room B",
                        areaM2: 16.0,
                        wallAreaM2: 11.0,
                        wallUValueWPerM2K: 0.62,
                        peopleCount: 1,
                        equipmentLoadW: 80.0,
                        lightingLoadW: 45.0)
                ],
                outdoorTemperatureC: 34.0,
                initialIndoorTemperatureC: 26.0));

        Assert.True(result.IsSuccess, result.Error);

        var simulation = result.Value;

        Assert.Equal(12, simulation.MonthlySummaries.Count);

        AssertClose(
            simulation.MonthlySummaries.Sum(month => month.InternalGainsKWh),
            simulation.AnnualInternalGainsKWh);

        AssertClose(
            simulation.MonthlySummaries.Sum(month => month.SolarGainsKWh),
            simulation.AnnualSolarGainsKWh);

        AssertClose(
            simulation.MonthlySummaries.Sum(month => month.TotalGainsKWh),
            simulation.AnnualTotalGainsKWh);

        Assert.True(
            simulation.MonthlySummaries.All(month => month.PeakHeatingLoadW <= simulation.PeakHeatingLoadW + Tolerance));

        Assert.True(
            simulation.MonthlySummaries.All(month => month.PeakCoolingLoadW <= simulation.PeakCoolingLoadW + Tolerance));
    }

    [Fact]
    public void ApplicationIntegrationHardeningFixtures_AreManifestScopedAndUseManualIntegrationSource()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixApplicationIntegrationHardeningManifest.json");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));

        var fixturePaths = document.RootElement
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => Path.Combine(item!.Split('/').Prepend(repoRoot).ToArray()))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(5, fixturePaths.Length);

        foreach (var fixturePath in fixturePaths)
        {
            Assert.True(File.Exists(fixturePath), $"Fixture was not found: {fixturePath}");

            using var fixtureDocument = JsonDocument.Parse(File.ReadAllText(fixturePath));
            var root = fixtureDocument.RootElement;

            Assert.Equal(
                "ManualEngineeringIntegrationAnchor",
                root.GetProperty("sourceType").GetString());

            Assert.Equal(
                "ApplicationIntegrationHardening",
                root.GetProperty("scope").GetString());

            Assert.StartsWith(
                "APPLICATION-ISO52016-MATRIX-INTEGRATION-",
                root.GetProperty("id").GetString(),
                StringComparison.Ordinal);
        }
    }

    private static Iso52016BuildingSimulationFacadeRequest CreateFacadeRequest(
        string buildingCode,
        IReadOnlyList<Room> rooms,
        double outdoorTemperatureC,
        double initialIndoorTemperatureC) =>
        new(
            BuildingCode: buildingCode,
            Rooms: rooms,
            AnnualClimateData: CreateAnnualClimateData(outdoorTemperatureC),
            LatitudeDegrees: 41.3,
            LongitudeDegrees: 69.2,
            TimeZoneOffset: TimeSpan.FromHours(5),
            Defaults: new Iso52016RoomSimulationDefaults(
                DefaultAirChangesPerHour: 0.0,
                DefaultHeatRecoveryEfficiency: 0.0),
            HeatingSetpointOverrideC: 21.0,
            CoolingSetpointOverrideC: 26.0,
            HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                InitialIndoorTemperatureC: initialIndoorTemperatureC));

    private static Room CreateRoomWithExternalWall(
        string name,
        double areaM2,
        double wallAreaM2,
        double wallUValueWPerM2K,
        int peopleCount = 0,
        double equipmentLoadW = 0.0,
        double lightingLoadW = 0.0)
    {
        var room = CreateRoomShell(
            name,
            areaM2,
            peopleCount,
            equipmentLoadW,
            lightingLoadW);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(wallAreaM2).Value,
            ThermalTransmittance.FromValue(wallUValueWPerM2K).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        return room;
    }

    private static Room CreateRoomShell(
        string name,
        double areaM2,
        int peopleCount = 0,
        double equipmentLoadW = 0.0,
        double lightingLoadW = 0.0)
    {
        var project = Project.Create("Matrix application integration hardening project").Value;
        var building = Building.Create("Matrix application integration hardening building", project).Value;
        var floor = Floor.Create("Matrix application integration hardening floor", building).Value;

        var room = Room.Create(
            name: name,
            area: Area.FromSquareMeters(areaM2).Value,
            heightM: 3.0,
            indoorTemp: Temperature.FromCelsius(20.0).Value,
            outdoorTemperatureOverride: null,
            floor: floor,
            peopleCount: peopleCount,
            equipmentLoad: Power.FromWatts(equipmentLoadW).Value,
            lightingLoad: Power.FromWatts(lightingLoadW).Value,
            type: RoomType.Office);

        Assert.True(room.IsSuccess, room.Error);

        return room.Value;
    }

    private static AnnualClimateData CreateAnnualClimateData(
        double outdoorTemperatureC)
    {
        var climateZone = ClimateZone.Create(
            "Matrix application integration climate zone",
            Temperature.FromCelsius(45).Value,
            Temperature.FromCelsius(-25).Value);

        Assert.True(climateZone.IsSuccess);

        var annualDataResult = AnnualClimateData.Create(
            climateZone.Value,
            year: 2024);

        Assert.True(annualDataResult.IsSuccess);

        var annualData = annualDataResult.Value;

        for (var hour = 0; hour < 8760; hour++)
        {
            var addResult = annualData.AddHourlyData(
                hourOfYear: hour,
                dryBulbTemp: outdoorTemperatureC,
                directSolar: 0.0,
                diffuseSolar: 0.0,
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

    private static Iso52016BuildingSimulationFacade CreateFacade() =>
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

    private static void AssertClose(
        double expected,
        double actual) =>
        Assert.InRange(
            actual,
            expected - Tolerance,
            expected + Tolerance);

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
}