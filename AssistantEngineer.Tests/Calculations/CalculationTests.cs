using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Persistence;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Iso52016;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Construction;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class CalculationTests
{
    [Fact]
    public async Task RoomCoolingLoadCalculatorUsesSimplifiedCalculationByDefault()
    {
        var calculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var room = DomainInvariantTests.CreateRoom(areaM2: 10);

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(nameof(CoolingLoadCalculationMethod.Simplified), result.CalculationMethod);
        Assert.Equal(1050, result.BaseRoomLoadW);
        Assert.Equal(1050, result.TotalHeatLoadW);
        Assert.Equal(1155, result.DesignCapacityW);
        Assert.Equal(24, result.HourlyHeatLoadW.Count);
    }

    [Fact]
    public async Task RoomCoolingLoadCalculatorSupportsIso52016CoolingLoadMethod()
    {
        var calculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var room = DomainInvariantTests.CreateRoom(areaM2: 20);
        DomainInvariantTests.AddExternalWall(room, CardinalDirection.West);
        Assert.True(room.AddWindow(
            Area.FromSquareMeters(4).Value,
            ThermalTransmittance.FromValue(1.8).Value,
            SolarHeatGainCoefficient.FromValue(0.55).Value,
            CardinalDirection.West).IsSuccess);

        var result = await calculator.CalculateAsync(room, CoolingLoadCalculationMethod.Iso52016);

        Assert.Equal(nameof(CoolingLoadCalculationMethod.Iso52016), result.CalculationMethod);
        Assert.Equal(24, result.HourlyHeatLoadW.Count);
        Assert.NotNull(result.PeakHour);
        Assert.InRange(result.PeakHour.Value, 0, 23);
        Assert.True(result.TotalHeatLoadW > 0);
    }

    [Fact]
    public async Task En12831HeatingLoadCalculatorSplitsTransmissionAndVentilationLosses()
    {
        var calculator = CalculationTestFactory.CreateHeatingLoadCalculator();
        var floor = DomainInvariantTests.CreateFloor();
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(-15).Value).Value;
        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(1.2).Value,
            CardinalDirection.North).IsSuccess);
        Assert.True(room.AddWindow(
            Area.FromSquareMeters(3).Value,
            ThermalTransmittance.FromValue(2).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.North).IsSuccess);

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(nameof(HeatingLoadCalculationMethod.En12831), result.CalculationMethod);
        Assert.True(result.TransmissionHeatLossW > 0);
        Assert.True(result.VentilationHeatLossW > 0);
        Assert.Equal(
            result.TransmissionHeatLossW + result.VentilationHeatLossW,
            result.TotalDesignHeatingLoadW,
            precision: 2);
    }

    [Fact]
    public async Task Iso52016CoolingLoadCalculatorUsesRoomSchedulesForInternalGains()
    {
        var calculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var floor = DomainInvariantTests.CreateFloor();
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(200).Value).Value;
        var zeroSchedule = HourlySchedule.Create("Off", Enumerable.Repeat(0.0, 24).ToArray()).Value;
        Assert.True(room.SetOccupancySchedule(zeroSchedule).IsSuccess);
        Assert.True(room.SetEquipmentSchedule(zeroSchedule).IsSuccess);
        Assert.True(room.SetLightingSchedule(zeroSchedule).IsSuccess);

        var result = await calculator.CalculateAsync(room, CoolingLoadCalculationMethod.Iso52016);

        Assert.Equal(0, result.InternalHeatGainW);
        Assert.Equal(0, result.PeopleHeatGainW);
        Assert.Equal(0, result.EquipmentHeatGainW);
        Assert.Equal(0, result.LightingHeatGainW);
    }

    [Fact]
    public async Task Iso52016CoolingLoadCalculatorUsesClimateDataDryBulbProfile()
    {
        var project = DomainInvariantTests.CreateProject();
        var climateZone = ClimateZone.Create(
            "Summer climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var building = Building.Create("Building", project, climateZone).Value;
        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(25).Value).Value;
        DomainInvariantTests.AddExternalWall(room, CardinalDirection.South);
        var provider = new FixedIso52016ReferenceDataProvider(
            Enumerable.Range(0, 24)
                .Select(hour => hour is 12 or 13 ? 42.0 : 22.0)
                .ToArray());
        var calculator = new Iso52016CoolingLoadCalculator(
            new Iso52016CoolingLoadOptions(),
            provider,
            CalculationTestFactory.CreateProfileAggregator());

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(20, result.DeltaTemperatureC);
    }

    [Fact]
    public async Task En12831HeatingLoadCalculatorUsesVentilationParameters()
    {
        var calculator = CalculationTestFactory.CreateHeatingLoadCalculator();
        var floor = DomainInvariantTests.CreateFloor();
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(-15).Value).Value;
        var ventilation = VentilationParameters.Create(1.0).Value;
        Assert.True(room.SetVentilationParameters(ventilation).IsSuccess);

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(1.0, result.AirChangesPerHour);
        Assert.Equal(754.8, result.VentilationHeatLossW);
    }

    [Fact]
    public async Task En12831HeatingLoadCalculatorUsesBuildingClimateZoneWinterTemperature()
    {
        var calculator = CalculationTestFactory.CreateHeatingLoadCalculator();
        var project = DomainInvariantTests.CreateProject();
        var climateZone = ClimateZone.Create(
            "Cold climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-15).Value).Value;
        var building = Building.Create("Building", project, climateZone).Value;
        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(10).Value).Value;

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(-15, result.OutdoorDesignTemperatureC);
        Assert.Equal(37, result.DeltaTemperatureC);
        Assert.Equal(377.4, result.VentilationHeatLossW);
    }

    [Fact]
    public async Task En12831HeatingLoadCalculatorUsesConstructionAssemblyUValue()
    {
        var calculator = CalculationTestFactory.CreateHeatingLoadCalculator();
        var floor = DomainInvariantTests.CreateFloor();
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(-15).Value).Value;
        var wall = room.AddWall(
            Area.FromSquareMeters(10).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(2).Value,
            CardinalDirection.North).Value;
        var material = Material.Create("Insulation", 0.04, 30, 1400).Value;
        var assembly = ConstructionAssembly.Create("Insulated wall").Value;
        Assert.True(assembly.AddLayer(material, 0.1).IsSuccess);
        Assert.True(wall.SetConstructionAssembly(assembly).IsSuccess);

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(138.58, result.TransmissionHeatLossW);
    }

    private sealed class FixedIso52016ReferenceDataProvider : IIso52016ReferenceDataProvider
    {
        private readonly IReadOnlyList<double> _outdoorTemperatureProfile;

        public FixedIso52016ReferenceDataProvider(IReadOnlyList<double> outdoorTemperatureProfile)
        {
            _outdoorTemperatureProfile = outdoorTemperatureProfile;
        }

        public Task<IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>>> GetSolarRadiationAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>>>(
                new Dictionary<CardinalDirection, IReadOnlyList<double>>());

        public Task<IReadOnlyList<double>?> GetOutdoorTemperatureProfileAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<double>?>(_outdoorTemperatureProfile);

        public Task<bool> HasClimateDataAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public double GetDefaultSolarRadiation(CardinalDirection orientation) => 0;

        public double GetPeopleHeatGain(RoomType roomType) => 0;
    }
}


