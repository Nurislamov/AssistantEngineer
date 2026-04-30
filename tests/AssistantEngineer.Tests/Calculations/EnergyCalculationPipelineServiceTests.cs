using System.Reflection;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Models.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations;

public class EnergyCalculationPipelineServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 30, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RoomLoadApplicationPathUsesRoomLoadEngineAndReturnsDeterministicBreakdown()
    {
        var building = CreateDeterministicBuilding();
        var service = CreateService(building);

        var heating = await service.CalculateRoomHeatingLoadAsync(1);
        var cooling = await service.CalculateRoomCoolingLoadAsync(1);

        Assert.True(heating.IsSuccess, heating.Error);
        Assert.True(cooling.IsSuccess, cooling.Error);
        Assert.Contains("Energy Calculation Parity", heating.Value.CalculationMethod);
        Assert.Contains("Energy Calculation Parity", cooling.Value.CalculationMethod);
        Assert.Equal("En12831", heating.Value.RequestedMethod);
        Assert.Equal("Simplified", cooling.Value.RequestedMethod);
        Assert.Equal("EnergyCalculationParityDesignPoint", heating.Value.ActualMethod);
        Assert.Equal("EnergyCalculationParityDesignPoint", cooling.Value.ActualMethod);
        Assert.Equal(1750, heating.Value.HeatingLoadW, precision: 2);
        Assert.Equal(87.5, heating.Value.HeatingLoadWPerM2, precision: 2);
        Assert.Equal(1000, heating.Value.Breakdown!.TransmissionW + heating.Value.Breakdown.WindowTransmissionW + heating.Value.Breakdown.GroundW, precision: 2);
        Assert.Equal(500, heating.Value.Breakdown.VentilationW, precision: 2);
        Assert.Equal(250, heating.Value.Breakdown.InfiltrationW, precision: 2);
        Assert.Equal(2000, cooling.Value.CoolingLoadW, precision: 2);
        Assert.Equal(300, cooling.Value.Breakdown!.TransmissionW + cooling.Value.Breakdown.WindowTransmissionW + cooling.Value.Breakdown.GroundW, precision: 2);
        Assert.Equal(600, cooling.Value.Breakdown.SolarW, precision: 2);
        Assert.Equal(500, cooling.Value.Breakdown.InternalGainsW, precision: 2);
        Assert.Equal(400, cooling.Value.Breakdown.VentilationW, precision: 2);
        Assert.Equal(200, cooling.Value.Breakdown.InfiltrationW, precision: 2);
        Assert.Contains(cooling.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarGains.ReferenceByOrientationFallback");
        Assert.Contains(cooling.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "InternalGains.DesignPointFullScheduleFactor");
        Assert.DoesNotContain(cooling.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "Ventilation.DefaultAirChangesPerHourUsed");
    }

    [Fact]
    public async Task RequestedCalculationMethodIsReportedSeparatelyFromActualPipelineMethod()
    {
        var building = CreateDeterministicBuilding();
        var service = CreateService(building);

        var cooling = await service.CalculateRoomCoolingLoadAsync(1, CoolingLoadCalculationMethod.Iso52016);
        var heating = await service.CalculateRoomHeatingLoadAsync(1, HeatingLoadCalculationMethod.En12831);

        Assert.True(cooling.IsSuccess, cooling.Error);
        Assert.True(heating.IsSuccess, heating.Error);
        Assert.Equal("Iso52016", cooling.Value.RequestedMethod);
        Assert.Equal("En12831", heating.Value.RequestedMethod);
        Assert.Equal("EnergyCalculationParityDesignPoint", cooling.Value.ActualMethod);
        Assert.Equal("EnergyCalculationParityDesignPoint", heating.Value.ActualMethod);
        Assert.Contains(cooling.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "CalculationMethod.ApiCompatibility" &&
            diagnostic.Message.Contains("Energy Calculation Parity design-point pipeline", StringComparison.Ordinal));
        Assert.Contains(heating.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "CalculationMethod.ApiCompatibility" &&
            diagnostic.Message.Contains("Energy Calculation Parity design-point pipeline", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BuildingAggregationUsesLoadAggregationEngineDesignPointSum()
    {
        var building = CreateDeterministicBuilding();
        var service = CreateService(building);
        var rooms = building.Floors.SelectMany(floor => floor.Rooms).ToArray();
        var roomCooling = new List<RoomCalculationResult>();
        var roomHeating = new List<RoomHeatingLoadResult>();

        foreach (var room in rooms)
        {
            roomCooling.Add((await service.CalculateRoomCoolingLoadAsync(room.Id)).Value);
            roomHeating.Add((await service.CalculateRoomHeatingLoadAsync(room.Id)).Value);
        }

        var buildingCooling = await service.CalculateBuildingCoolingLoadAsync(building.Id);
        var buildingHeating = await service.CalculateBuildingHeatingLoadAsync(building.Id);
        var floorCooling = await service.CalculateFloorCoolingLoadAsync(building.Floors.Single().Id);
        var floorHeating = await service.CalculateFloorHeatingLoadAsync(building.Floors.Single().Id);

        Assert.True(buildingCooling.IsSuccess, buildingCooling.Error);
        Assert.True(buildingHeating.IsSuccess, buildingHeating.Error);
        Assert.True(floorCooling.IsSuccess, floorCooling.Error);
        Assert.True(floorHeating.IsSuccess, floorHeating.Error);
        Assert.Contains("Load Aggregation", buildingCooling.Value.CalculationMethod);
        Assert.Equal(roomCooling.Sum(room => room.CoolingLoadW), buildingCooling.Value.CoolingLoadW, precision: 2);
        Assert.Equal(roomHeating.Sum(room => room.HeatingLoadW), buildingHeating.Value.HeatingLoadW, precision: 2);
        Assert.Equal(roomCooling.Sum(room => room.CoolingLoadW), floorCooling.Value.CoolingLoadW, precision: 2);
        Assert.Equal(roomHeating.Sum(room => room.HeatingLoadW), floorHeating.Value.HeatingLoadW, precision: 2);
    }

    [Fact]
    public async Task MissingRoomVentilationUsesDefaultAchWithDiagnostics()
    {
        var building = CreateDeterministicBuilding(includeVentilation: false);
        var service = CreateService(building);

        var result = await service.CalculateRoomHeatingLoadAsync(1);

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value.Breakdown!.VentilationW > 0);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "Ventilation.DefaultAirChangesPerHourUsed" &&
            diagnostic.Message.Contains("0.5", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvalidDefaultAchIsExposedAsDiagnosticError()
    {
        var building = CreateDeterministicBuilding(includeVentilation: false);
        var preferences = CreatePreferences(defaultAch: -0.5);
        var service = CreateService(building, preferences: preferences);

        var result = await service.CalculateRoomHeatingLoadAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("Ventilation.InvalidDefaultAirChangesPerHour", result.Error);
    }

    [Fact]
    public async Task SolarGainsUseAnnualClimateDataWhenAvailable()
    {
        var building = CreateDeterministicBuilding();
        var annualClimate = CreateAnnualClimateData(building.ClimateZone!);
        var service = CreateService(
            building,
            annualClimateDataProvider: new FakeAnnualClimateDataProvider(annualClimate),
            solarRadiationService: new FixedSolarRadiationService(240));

        var result = await service.CalculateRoomCoolingLoadAsync(1);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(600, result.Value.Breakdown!.SolarW, precision: 2);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarGains.IrradianceSource" &&
            diagnostic.Message.Contains("AnnualClimateData", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarGains.ReferenceByOrientationFallback");
    }

    [Fact]
    public async Task GroundBoundaryUsesResolvedGroundTemperatureWhenMetadataAndClimateAreAvailable()
    {
        var building = CreateDeterministicBuilding(
            includeGroundWall: true,
            includeGroundMetadata: true);
        var annualClimate = CreateAnnualClimateData(building.ClimateZone!);
        var service = CreateService(
            building,
            annualClimateDataProvider: new FakeAnnualClimateDataProvider(annualClimate),
            groundTemperatureService: new FixedGroundTemperatureService(12));

        var room = await service.CalculateRoomHeatingLoadAsync(1);
        var buildingLoad = await service.CalculateBuildingHeatingLoadAsync(building.Id);

        Assert.True(room.IsSuccess, room.Error);
        Assert.True(buildingLoad.IsSuccess, buildingLoad.Error);
        Assert.Equal(80, room.Value.Breakdown!.GroundW, precision: 2);
        Assert.True(buildingLoad.Value.ComponentBreakdown!.GroundW >= 80);
        Assert.Contains(room.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "GroundContact.GroundTemperatureProfileUsed");
        Assert.DoesNotContain(room.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "GroundContact.MetadataMissing");
    }

    [Fact]
    public async Task GroundBoundaryWithoutMetadataReturnsDiagnostic()
    {
        var building = CreateDeterministicBuilding(
            includeGroundWall: true,
            includeGroundMetadata: false);
        var service = CreateService(building);

        var result = await service.CalculateRoomHeatingLoadAsync(1);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "GroundContact.MetadataMissing");
    }

    [Fact]
    public async Task DesignPointInternalGainsReportFullScheduleFactorWhenSchedulesExist()
    {
        var building = CreateDeterministicBuilding(includeSchedules: true);
        var service = CreateService(building);

        var result = await service.CalculateRoomCoolingLoadAsync(1);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "InternalGains.DesignPointFullScheduleFactorWithSchedules");
    }

    [Fact]
    public async Task EnergyBalanceApplicationPathUsesAnnualEnergyEngineAdapterWithDiagnostics()
    {
        var building = CreateDeterministicBuilding();
        var service = CreateService(building);

        var result = await service.CalculateBuildingEnergyBalanceAsync(building.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("EnergyCalculationParityAnnualAggregationAdapter", result.Value.ActualMethod);
        Assert.Equal("MonthlyBalanceAdapter", result.Value.EnergyDataSource);
        Assert.False(result.Value.IsTrueHourly8760);
        Assert.Equal(
            result.Value.MonthlyBalances.Sum(month => month.HeatingDemandKWh + month.CoolingDemandKWh),
            result.Value.AnnualTotalDemandKWh,
            precision: 6);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.SyntheticWeather" &&
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.MonthlyBalanceAdapter" &&
            diagnostic.Message.Contains("not a true hourly 8760", StringComparison.Ordinal));
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "CalculationMethod.ApiCompatibility");
    }

    [Fact]
    public async Task EnergyBalanceApplicationPathUsesHourlyRecordsWhenSourceProvidesThem()
    {
        var building = CreateDeterministicBuilding();
        var hourlyRecords = CreateAnnualHourlyRecords(heatingLoadW: 100, coolingLoadW: 50);
        var service = CreateService(
            building,
            energyCalculator: new FixedBuildingEnergyCalculator(hourlyRecords));

        var result = await service.CalculateBuildingEnergyBalanceAsync(building.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("HourlySimulation", result.Value.EnergyDataSource);
        Assert.True(result.Value.IsTrueHourly8760);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlySimulationSource");
    }

    [Fact]
    public async Task RoomEquipmentSizingUsesRoomLoadSafetyFactorAndExplainsAcceptedRejectedCandidates()
    {
        var building = CreateDeterministicBuilding();
        var catalog = new FakeCatalogSizingProvider([
            new CoolingEquipmentCatalogSizingCandidate(1, "Acme", "DX", "Wall", "TooSmall-2000", 2.0),
            new CoolingEquipmentCatalogSizingCandidate(2, "Acme", "DX", "Wall", "Fit-2500", 2.5)
        ]);
        var service = CreateService(building, catalog);

        var result = await service.CalculateRoomEquipmentSizingAsync(
            1,
            "DX",
            "Wall",
            CoolingLoadCalculationMethod.Simplified);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(2000, result.Value.RequiredCoolingCapacityW, precision: 2);
        Assert.Equal(1750, result.Value.RequiredHeatingCapacityW, precision: 2);
        Assert.Equal(1.1, result.Value.SafetyFactor, precision: 2);
        Assert.Equal(2200, result.Value.RequiredCoolingCapacityWithReserveW, precision: 2);
        Assert.Equal(1925, result.Value.RequiredHeatingCapacityWithReserveW, precision: 2);
        Assert.Equal(2, result.Value.BestMatch!.EquipmentId);
        Assert.Contains(result.Value.RecommendedEquipment, item => item.EquipmentId == 2);
        Assert.Contains(result.Value.RejectedEquipment, item =>
            item.EquipmentId == 1 &&
            item.Reasons.Contains("insufficient cooling capacity"));
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "EquipmentSizing.HeatingCapacityUnavailable");
    }

    [Fact]
    public async Task RoomEquipmentSizingRejectsCandidateWithInsufficientHeatingCapacity()
    {
        var building = CreateDeterministicBuilding();
        var catalog = new FakeCatalogSizingProvider([
            new CoolingEquipmentCatalogSizingCandidate(1, "Acme", "DX", "Wall", "HeatingSmall-2500-1800", 2.5, 1.8),
            new CoolingEquipmentCatalogSizingCandidate(2, "Acme", "DX", "Wall", "HeatingFit-2500-2000", 2.5, 2.0)
        ]);
        var service = CreateService(building, catalog);

        var result = await service.CalculateRoomEquipmentSizingAsync(
            1,
            "DX",
            "Wall",
            CoolingLoadCalculationMethod.Simplified);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(2, result.Value.BestMatch!.EquipmentId);
        Assert.Contains(result.Value.RecommendedEquipment, item => item.EquipmentId == 2);
        Assert.Contains(result.Value.RejectedEquipment, item =>
            item.EquipmentId == 1 &&
            item.Reasons.Contains("insufficient heating capacity"));
        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "EquipmentSizing.HeatingCapacityUnavailable");
    }

    [Fact]
    public async Task RoomEquipmentSizingReturnsDiagnosticsForEmptyCatalog()
    {
        var building = CreateDeterministicBuilding();
        var service = CreateService(building, new FakeCatalogSizingProvider([]));

        var result = await service.CalculateRoomEquipmentSizingAsync(
            1,
            "DX",
            "Wall",
            CoolingLoadCalculationMethod.Simplified);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Null(result.Value.BestMatch);
        Assert.Contains(result.Value.Diagnostics, diagnostic => diagnostic.Code == "EquipmentSizing.NoEquipmentFound");
    }

    [Fact]
    public async Task MissingCriticalClimateInputsReturnValidation()
    {
        var building = CreateDeterministicBuilding(hasClimateZone: false);
        var service = CreateService(building);

        var result = await service.CalculateRoomHeatingLoadAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("climate zone", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static EnergyCalculationPipelineService CreateService(
        Building building,
        ICoolingEquipmentCatalogSizingProvider? catalog = null,
        IAnnualClimateDataProvider? annualClimateDataProvider = null,
        IGroundTemperatureService? groundTemperatureService = null,
        ISolarRadiationService? solarRadiationService = null,
        CalculationPreferences? preferences = null,
        IBuildingEnergyCalculator? energyCalculator = null)
    {
        var repository = new BuildingGraphRepositoryStub(building, preferences);
        var timeProvider = new FixedTimeProvider(FixedNow);

        return new EnergyCalculationPipelineService(
            repository,
            repository,
            repository,
            repository,
            new RoomLoadCalculationEngine(timeProvider: timeProvider),
            new LoadAggregationEngine(timeProvider),
            new AnnualEnergyBalanceEngine(timeProvider),
            new EquipmentSizingEngine(timeProvider),
            energyCalculator ?? new FixedBuildingEnergyCalculator(),
            new CoolingLoadReferenceData(),
            Options.Create(new CoolingLoadCalculationOptions()),
            Options.Create(new En12831HeatingLoadOptions()),
            timeProvider,
            catalog ?? new FakeCatalogSizingProvider([]),
            annualClimateDataProvider,
            groundTemperatureService,
            solarRadiationService,
            Options.Create(new Iso52016EnergyNeedOptions()));
    }

    private static Building CreateDeterministicBuilding(
        bool hasClimateZone = true,
        bool includeVentilation = true,
        bool includeGroundWall = false,
        bool includeGroundMetadata = true,
        bool includeSchedules = false)
    {
        var project = DomainInvariantTests.CreateProject("Pipeline project");
        SetId(project, 100);

        var climateZone = hasClimateZone
            ? ClimateZone.Create(
                "Deterministic climate",
                Temperature.FromCelsius(36).Value,
                Temperature.FromCelsius(0).Value).Value
            : null;
        if (climateZone is not null)
            SetId(climateZone, 200);

        var building = Building.Create("Pipeline building", project, climateZone).Value;
        SetId(building, 10);
        Assert.True(project.AddBuilding(building).IsSuccess);

        var floor = building.AddFloor("Level 1").Value;
        SetId(floor, 11);

        var room = floor.AddRoom(
            "Deterministic room",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: null,
            peopleCount: 4,
            type: RoomType.Office).Value;
        SetId(room, 1);

        var adjacent = floor.AddRoom(
            "Adjacent unheated room",
            Area.FromSquareMeters(5).Value,
            3,
            Temperature.FromCelsius(-5).Value,
            outdoorTemperatureOverride: null,
            peopleCount: 0,
            type: RoomType.Corridor).Value;
        SetId(adjacent, 2);

        if (includeVentilation)
        {
            Assert.True(room.SetVentilationParameters(
                VentilationParameters.Create(
                    airChangesPerHour: 500.0 / (1.2 * 1005.0 * (60.0 / 3600.0) * 20.0),
                    heatRecoveryEfficiency: 0,
                    infiltrationAirChangesPerHour: 250.0 / (1.2 * 1005.0 * (60.0 / 3600.0) * 20.0),
                    windExposureFactor: 0,
                    stackCoefficient: 0,
                    windCoefficient: 0).Value).IsSuccess);
        }

        Assert.True(adjacent.SetVentilationParameters(VentilationParameters.Create(0, 0).Value).IsSuccess);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(20).Value,
            ThermalTransmittance.FromValue(0.935).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);
        Assert.True(room.AddWindow(
            Area.FromSquareMeters(5).Value,
            ThermalTransmittance.FromValue(0.01).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.South).IsSuccess);
        Assert.True(room.AddWall(
            Area.FromSquareMeters(1).Value,
            ThermalTransmittance.FromValue(25).Value,
            CardinalDirection.North,
            WallBoundaryType.AdjacentUnconditioned,
            adjacent).IsSuccess);

        if (includeGroundWall)
        {
            Assert.True(room.AddWall(
                Area.FromSquareMeters(10).Value,
                ThermalTransmittance.FromValue(1).Value,
                CardinalDirection.North,
                WallBoundaryType.Ground).IsSuccess);

            if (includeGroundMetadata)
            {
                Assert.True(room.SetGroundContactMetadata(
                    GroundContactMetadata.Create(
                        GroundContactType.SlabOnGround,
                        exposedPerimeterM: 12,
                        burialDepthM: 0,
                        wallHeightBelowGradeM: 0,
                        horizontalInsulationWidthM: 0,
                        perimeterInsulationDepthM: 0,
                        underfloorVentilationAirChangesPerHour: 0).Value).IsSuccess);
            }
        }

        if (includeSchedules)
        {
            var schedule = HourlySchedule.Create(
                "Occupied",
                Enumerable.Repeat(0.75, 24).ToArray()).Value;
            Assert.True(room.SetOccupancySchedule(schedule).IsSuccess);
            Assert.True(room.SetEquipmentSchedule(schedule).IsSuccess);
            Assert.True(room.SetLightingSchedule(schedule).IsSuccess);
        }

        return building;
    }

    private static CalculationPreferences CreatePreferences(double defaultAch = 0.5)
    {
        var preferences = CalculationPreferences.Create(1.1, 1.1).Value;
        SetBackingField(preferences, nameof(CalculationPreferences.Iso52016DefaultAirChangesPerHour), defaultAch);
        return preferences;
    }

    private static AnnualClimateData CreateAnnualClimateData(ClimateZone climateZone)
    {
        var annual = AnnualClimateData.Create(climateZone, 2020).Value;
        SetId(annual, 300);
        Assert.True(annual.AddHourlyData(
            hourOfYear: 12,
            dryBulbTemp: 28,
            directSolar: 700,
            diffuseSolar: 120).IsSuccess);
        return annual;
    }

    private static IReadOnlyList<AnnualEnergyBalanceHourInput> CreateAnnualHourlyRecords(
        double heatingLoadW,
        double coolingLoadW) =>
        Enumerable.Range(0, 8760)
            .Select(hour => new AnnualEnergyBalanceHourInput(
                HourIndex: hour,
                Month: MonthFromHour(hour),
                HeatingLoadW: heatingLoadW,
                CoolingLoadW: coolingLoadW,
                HourDurationH: 1.0))
            .ToArray();

    private static int MonthFromHour(int hour)
    {
        var dayOfYear = hour / 24;
        var daysPerMonth = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        var accumulated = 0;
        for (var month = 1; month <= 12; month++)
        {
            accumulated += daysPerMonth[month - 1];
            if (dayOfYear < accumulated)
                return month;
        }

        return 12;
    }

    private static void SetId(object entity, int id)
    {
        SetBackingField(entity, "Id", id);
    }

    private static void SetBackingField(object entity, string propertyName, object? value)
    {
        var field = entity.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, value);
    }

    private sealed class BuildingGraphRepositoryStub :
        IRoomRepository,
        IFloorRepository,
        IBuildingRepository,
        ICalculationPreferencesRepository
    {
        private readonly Building _building;
        private readonly CalculationPreferences _preferences;

        public BuildingGraphRepositoryStub(Building building, CalculationPreferences? preferences = null)
        {
            _building = building;
            _preferences = preferences ?? CalculationPreferences.Create(1.1, 1.1).Value;
        }

        Task<Room?> IRoomRepository.GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(FindRoom(id));

        Task<Room?> IRoomRepository.GetForCalculationAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithWindowsAndWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithVentilationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(_building.Floors.SelectMany(floor => floor.Rooms).ToList());

        Task<IReadOnlyList<Room>> IRoomRepository.ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Room>>(_building.Id == buildingId
                ? _building.Floors.SelectMany(floor => floor.Rooms).ToList()
                : []);

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id) is not null);

        public Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Window>>(FindRoom(roomId)?.Windows.ToList() ?? []);

        public Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Wall>>(FindRoom(roomId)?.Walls.ToList() ?? []);

        public void Add(Room room) => throw new NotSupportedException();
        public void Remove(Room room) => throw new NotSupportedException();
        public void RemoveWindow(Window window) => throw new NotSupportedException();
        public void RemoveWall(Wall wall) => throw new NotSupportedException();

        Task<Floor?> IFloorRepository.GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(FindFloor(id));

        public Task<Floor?> GetWithRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindFloor(id));

        Task<Floor?> IFloorRepository.GetForCalculationAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(FindFloor(id));

        Task<IReadOnlyList<Floor>> IFloorRepository.ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Floor>>(_building.Id == buildingId ? _building.Floors.ToList() : []);

        public void Add(Floor floor) => throw new NotSupportedException();
        public void Remove(Floor floor) => throw new NotSupportedException();

        public Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.ThermalZones.Any(zone => zone.Id == thermalZoneId) ? _building : null);

        Task<Building?> IBuildingRepository.GetForCalculationAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Building>>(_building.ProjectId == projectId ? [_building] : []);

        public void Add(Building building) => throw new NotSupportedException();
        public void Remove(Building building) => throw new NotSupportedException();

        public Task<Building?> GetForValidationAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<CalculationPreferences?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(_preferences);

        private Room? FindRoom(int id) =>
            _building.Floors.SelectMany(floor => floor.Rooms).FirstOrDefault(room => room.Id == id);

        private Floor? FindFloor(int id) =>
            _building.Floors.FirstOrDefault(floor => floor.Id == id);
    }

    private sealed class FixedBuildingEnergyCalculator : IBuildingEnergyCalculator
    {
        private readonly IReadOnlyList<AnnualEnergyBalanceHourInput>? _hourlyRecords;

        public FixedBuildingEnergyCalculator(IReadOnlyList<AnnualEnergyBalanceHourInput>? hourlyRecords = null)
        {
            _hourlyRecords = hourlyRecords;
        }

        public Task<BuildingEnergyBalanceResult> CalculateAsync(
            Building building,
            CoolingLoadCalculationMethod coolingMethod,
            HeatingLoadCalculationMethod heatingMethod,
            CalculationPreferences? preferences = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BuildingEnergyBalanceResult
            {
                BuildingId = building.Id,
                BuildingName = building.Name,
                CoolingCalculationMethod = coolingMethod.ToString(),
                HeatingCalculationMethod = heatingMethod.ToString(),
                HourlyBalanceRecords = _hourlyRecords?.ToList() ?? [],
                AnnualCoolingDemandKWh = 50,
                AnnualHeatingDemandKWh = 100,
                AnnualTotalDemandKWh = 150,
                MonthlyBalances =
                [
                    new MonthlyEnergyBalance { Month = 1, HeatingDemandKWh = 100, CoolingDemandKWh = 0 },
                    new MonthlyEnergyBalance { Month = 7, HeatingDemandKWh = 0, CoolingDemandKWh = 50 }
                ]
            });
    }

    private sealed class FakeCatalogSizingProvider : ICoolingEquipmentCatalogSizingProvider
    {
        private readonly IReadOnlyList<CoolingEquipmentCatalogSizingCandidate> _candidates;

        public FakeCatalogSizingProvider(IReadOnlyList<CoolingEquipmentCatalogSizingCandidate> candidates)
        {
            _candidates = candidates;
        }

        public Task<IReadOnlyList<CoolingEquipmentCatalogSizingCandidate>> ListActiveCoolingCandidatesAsync(
            string systemType,
            string unitType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_candidates
                .Where(candidate =>
                    candidate.SystemType == systemType &&
                    candidate.UnitType == unitType)
                .ToList()
                as IReadOnlyList<CoolingEquipmentCatalogSizingCandidate>);
    }

    private sealed class FakeAnnualClimateDataProvider : IAnnualClimateDataProvider
    {
        private readonly AnnualClimateData _annualClimateData;

        public FakeAnnualClimateDataProvider(AnnualClimateData annualClimateData)
        {
            _annualClimateData = annualClimateData;
        }

        public Task<AnnualClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AnnualClimateData?>(_annualClimateData.ClimateZoneId == climateZoneId
                ? _annualClimateData
                : null);
    }

    private sealed class FixedGroundTemperatureService : IGroundTemperatureService
    {
        private readonly double _temperatureC;

        public FixedGroundTemperatureService(double temperatureC)
        {
            _temperatureC = temperatureC;
        }

        public double[] BuildHourlyProfile(IReadOnlyList<HourlyClimateData> hourlyClimateData) =>
            Enumerable.Repeat(_temperatureC, 8760).ToArray();

        public double GetMonthlyAverageTemperature(IReadOnlyList<HourlyClimateData> hourlyClimateData, int month) =>
            _temperatureC;
    }

    private sealed class FixedSolarRadiationService : ISolarRadiationService
    {
        private readonly double _irradianceWPerM2;

        public FixedSolarRadiationService(double irradianceWPerM2)
        {
            _irradianceWPerM2 = irradianceWPerM2;
        }

        public double CalculateVerticalSurfaceRadiation(
            HourlyClimateData hourlyData,
            CardinalDirection orientation,
            double latitude,
            int dayOfYear,
            int hour) =>
            _irradianceWPerM2;
    }
}
