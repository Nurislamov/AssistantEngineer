using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Services;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class BuildingEnergyCalculatorTests
{
    [Fact]
    public async Task CalculateAsyncAggregatesMonthlyAndAnnualDemand()
    {
        var climateZone = ClimateZone.Create(
            "Mixed climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;
        var project = DomainInvariantTests.CreateProject();
        var building = Building.Create("Building", project, climateZone).Value;
        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value).Value;
        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(1.2).Value,
            CardinalDirection.South).IsSuccess);

        var climateData = ClimateData.Create(climateZone, month: 1, dayOfMonth: 15, dailyTemperatureRange: 10).Value;
        var calculator = new Iso52016BuildingEnergyCalculator(
            CalculationTestFactory.CreateRoomCoolingLoadCalculator(),
            CalculationTestFactory.CreateHeatingLoadCalculator(),
            new ClimateDataRepositoryStub(climateData));

        var result = await calculator.CalculateAsync(
            building,
            CoolingLoadCalculationMethod.Simplified,
            HeatingLoadCalculationMethod.En12831);

        Assert.Single(result.MonthlyBalances);
        Assert.Equal(1, result.MonthlyBalances[0].Month);
        Assert.True(result.MonthlyBalances[0].CoolingDemandKWh > 0);
        Assert.True(result.MonthlyBalances[0].HeatingDemandKWh > 0);
        Assert.Equal(
            result.AnnualCoolingDemandKWh + result.AnnualHeatingDemandKWh,
            result.AnnualTotalDemandKWh,
            precision: 2);
    }

    [Fact]
    public async Task CalculateAsyncReusesRoomCalculationsAcrossClimateMonths()
    {
        var climateZone = ClimateZone.Create(
            "Mixed climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;
        var project = DomainInvariantTests.CreateProject();
        var building = Building.Create("Building", project, climateZone).Value;
        var floor = building.AddFloor("Level 1").Value;
        _ = floor.AddRoom(
            "Office 101",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value).Value;
        _ = floor.AddRoom(
            "Office 102",
            Area.FromSquareMeters(25).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value).Value;
        var coolingCalculator = new CountingRoomCoolingLoadCalculator();
        var heatingCalculator = new CountingRoomHeatingLoadCalculator();
        var calculator = new Iso52016BuildingEnergyCalculator(
            coolingCalculator,
            heatingCalculator,
            new ClimateDataRepositoryStub(
                ClimateData.Create(climateZone, month: 1, dayOfMonth: 15, dailyTemperatureRange: 10).Value,
                ClimateData.Create(climateZone, month: 2, dayOfMonth: 15, dailyTemperatureRange: 10).Value));

        var result = await calculator.CalculateAsync(
            building,
            CoolingLoadCalculationMethod.Simplified,
            HeatingLoadCalculationMethod.En12831);

        Assert.Equal(2, result.MonthlyBalances.Count);
        Assert.Equal(2, coolingCalculator.CallCount);
        Assert.Equal(2, heatingCalculator.CallCount);
    }

    [Fact]
    public async Task CalculateAsyncLoadsAvailableClimateMonthsOnce()
    {
        var climateZone = ClimateZone.Create(
            "Mixed climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;
        var project = DomainInvariantTests.CreateProject();
        var building = Building.Create("Building", project, climateZone).Value;
        var floor = building.AddFloor("Level 1").Value;
        _ = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value).Value;
        var climateDataRepository = new ClimateDataRepositoryStub(
            ClimateData.Create(climateZone, month: 1, dayOfMonth: 15, dailyTemperatureRange: 10).Value,
            ClimateData.Create(climateZone, month: 7, dayOfMonth: 15, dailyTemperatureRange: 10).Value);
        var calculator = new Iso52016BuildingEnergyCalculator(
            new CountingRoomCoolingLoadCalculator(),
            new CountingRoomHeatingLoadCalculator(),
            climateDataRepository);

        var result = await calculator.CalculateAsync(
            building,
            CoolingLoadCalculationMethod.Simplified,
            HeatingLoadCalculationMethod.En12831);

        Assert.Equal([1, 7], result.MonthlyBalances.Select(balance => balance.Month));
        Assert.Equal(1, climateDataRepository.AvailableMonthsCallCount);
        Assert.Equal(0, climateDataRepository.FullClimateDataCallCount);
    }

    [Fact]
    public async Task CalculateAsyncUsesAnnualIso52016EnergyNeedsWhenWeatherDataExists()
    {
        var climateZone = ClimateZone.Create(
            "Annual climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;
        var project = DomainInvariantTests.CreateProject();
        var building = Building.Create("Building", project, climateZone).Value;
        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value,
            peopleCount: 1).Value;
        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(1.2).Value,
            CardinalDirection.South).IsSuccess);
        var climateDataRepository = new ClimateDataRepositoryStub();
        var annualCalculator = new Iso52016HourlySteadyStateCalculator(
            new AnnualClimateDataProviderStub(CreateAnnualClimateData(climateZone)),
            new SolarRadiationService());
        var calculator = new Iso52016BuildingEnergyCalculator(
            new CountingRoomCoolingLoadCalculator(),
            new CountingRoomHeatingLoadCalculator(),
            climateDataRepository,
            annualCalculator);

        var result = await calculator.CalculateAsync(
            building,
            CoolingLoadCalculationMethod.Iso52016,
            HeatingLoadCalculationMethod.En12831);

        Assert.Equal(12, result.MonthlyBalances.Count);
        Assert.True(result.AnnualHeatingDemandKWh > 0);
        Assert.True(result.AnnualCoolingDemandKWh > 0);
        Assert.Equal(0, climateDataRepository.AvailableMonthsCallCount);
        Assert.Equal(
            result.AnnualHeatingDemandKWh + result.AnnualCoolingDemandKWh,
            result.AnnualTotalDemandKWh,
            precision: 2);
    }

    [Fact]
    public async Task CalculateAsyncThrowsWhenClimateZoneIsMissing()
    {
        var building = DomainInvariantTests.CreateBuilding();
        var calculator = new Iso52016BuildingEnergyCalculator(
            CalculationTestFactory.CreateRoomCoolingLoadCalculator(),
            CalculationTestFactory.CreateHeatingLoadCalculator(),
            new ClimateDataRepositoryStub());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            calculator.CalculateAsync(
                building,
                CoolingLoadCalculationMethod.Simplified,
                HeatingLoadCalculationMethod.En12831));
    }

    private sealed class ClimateDataRepositoryStub : IClimateDataRepository
    {
        private readonly IReadOnlyList<ClimateData> _climateData;

        public ClimateDataRepositoryStub(params ClimateData?[] climateData) =>
            _climateData = climateData.Where(data => data is not null).Select(data => data!).ToArray();

        public int FullClimateDataCallCount { get; private set; }

        public int AvailableMonthsCallCount { get; private set; }

        public Task<ClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int month,
            CancellationToken cancellationToken = default)
        {
            FullClimateDataCallCount++;
            return Task.FromResult(
                _climateData.FirstOrDefault(data =>
                    climateZoneId == data.ClimateZoneId &&
                    month == data.Month));
        }

        public Task<IReadOnlyList<int>> GetAvailableMonthsForClimateZoneAsync(
            int climateZoneId,
            CancellationToken cancellationToken = default)
        {
            AvailableMonthsCallCount++;
            return Task.FromResult<IReadOnlyList<int>>(
                _climateData
                    .Where(data => data.ClimateZoneId == climateZoneId)
                    .Select(data => data.Month)
                    .OrderBy(month => month)
                    .ToArray());
        }
    }

    private sealed class CountingRoomCoolingLoadCalculator : IRoomCoolingLoadCalculator
    {
        public int CallCount { get; private set; }

        public Task<RoomCalculationResult> CalculateAsync(
            Room room,
            CalculationPreferences? preferences = null,
            CancellationToken cancellationToken = default) =>
            CalculateAsync(room, CoolingLoadCalculationMethod.Simplified, preferences, cancellationToken);

        public Task<RoomCalculationResult> CalculateAsync(
            Room room,
            CoolingLoadCalculationMethod method,
            CalculationPreferences? preferences = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new RoomCalculationResult
            {
                RoomId = room.Id,
                RoomName = room.Name,
                CalculationMethod = method.ToString(),
                HourlyHeatLoadW = Enumerable.Repeat(1000.0, 24).ToList()
            });
        }
    }

    private sealed class CountingRoomHeatingLoadCalculator : IRoomHeatingLoadCalculator
    {
        public int CallCount { get; private set; }

        public Task<RoomHeatingLoadResult> CalculateAsync(
            Room room,
            HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
            CalculationPreferences? preferences = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new RoomHeatingLoadResult
            {
                RoomId = room.Id,
                RoomName = room.Name,
                CalculationMethod = method.ToString(),
                TotalDesignHeatingLoadW = 1000,
                TotalDesignHeatingLoadKw = 1
            });
        }
    }

    private static AnnualClimateData CreateAnnualClimateData(ClimateZone climateZone)
    {
        var annualData = AnnualClimateData.Create(climateZone, year: 2020).Value;
        for (var hour = 0; hour < 8760; hour++)
        {
            var dryBulbTemp = hour < 4380 ? -5.0 : 35.0;
            Assert.True(annualData.AddHourlyData(
                hour,
                dryBulbTemp,
                directSolar: 0,
                diffuseSolar: 0).IsSuccess);
        }

        return annualData;
    }

    private sealed class AnnualClimateDataProviderStub : IAnnualClimateDataProvider
    {
        private readonly AnnualClimateData _annualData;

        public AnnualClimateDataProviderStub(AnnualClimateData annualData)
        {
            _annualData = annualData;
        }

        public Task<AnnualClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AnnualClimateData?>(
                climateZoneId == _annualData.ClimateZoneId && year == _annualData.Year
                    ? _annualData
                    : null);
    }
}


