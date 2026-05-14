using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

public sealed partial class EnergyCalculationPipelineService : IEnergyCalculationPipeline
{
    private const string RoomPipelineMethod = "Standard-Based Calculation / Application Room Load Pipeline";
    private const string AggregationPipelineMethod = "Standard-Based Calculation / Application Load Aggregation Pipeline";
    private const string ExternalReferenceValidationDesignPoint = "ExternalReferenceValidationDesignPoint";
    private const string ExternalReferenceValidationAnnualAggregationAdapter = "ExternalReferenceValidationAnnualAggregationAdapter";
    private readonly IRoomRepository _rooms;
    private readonly IFloorRepository _floors;
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly RoomLoadCalculationEngine _roomLoadEngine;
    private readonly LoadAggregationEngine _aggregationEngine;
    private readonly AnnualEnergyBalanceEngine _annualEnergyEngine;
    private readonly IBuildingEnergyCalculator _legacyEnergyCalculator;
    private readonly ICoolingLoadReferenceData _coolingReferenceData;
    private readonly IAnnualClimateDataProvider? _annualClimateDataProvider;
    private readonly IGroundTemperatureService? _groundTemperatureService;
    private readonly ISolarRadiationService? _solarRadiationService;
    private readonly CoolingLoadCalculationOptions _coolingOptions;
    private readonly En12831HeatingLoadOptions _heatingOptions;
    private readonly Iso52016EnergyNeedOptions _energyNeedOptions;
    private readonly ILogger<EnergyCalculationPipelineService> _logger;
    private readonly EnergyCalculationPipelineClimateContextBuilder _climateContextBuilder;
    private readonly EnergyCalculationPipelineAnnualInputAdapter _annualInputAdapter;
    private readonly EnergyCalculationPipelineRoomContextResolver _roomContextResolver;
    private readonly EnergyCalculationPipelineAggregationRoomAssembler _aggregationRoomAssembler;
    private readonly IEquipmentSizingCalculationUseCase _equipmentSizingCalculationUseCase;
    private readonly ISystemEnergyHandoffUseCase _systemEnergyHandoffUseCase;
    private readonly EnergyCalculationPipelineDiagnosticsPolicy _diagnosticsPolicy;
    private readonly EnergyCalculationPipelineBuildingHeatingResultAssembler _buildingHeatingResultAssembler;

    public EnergyCalculationPipelineService(
        IRoomRepository rooms,
        IFloorRepository floors,
        IBuildingRepository buildings,
        ICalculationPreferencesRepository preferences,
        RoomLoadCalculationEngine roomLoadEngine,
        LoadAggregationEngine aggregationEngine,
        AnnualEnergyBalanceEngine annualEnergyEngine,
        IEquipmentSizingCalculationUseCase equipmentSizingCalculationUseCase,
        ISystemEnergyHandoffUseCase systemEnergyHandoffUseCase,
        IBuildingEnergyCalculator legacyEnergyCalculator,
        ICoolingLoadReferenceData coolingReferenceData,
        IOptions<CoolingLoadCalculationOptions> coolingOptions,
        IOptions<En12831HeatingLoadOptions> heatingOptions,
        TimeProvider timeProvider,
        IAnnualClimateDataProvider? annualClimateDataProvider = null,
        IGroundTemperatureService? groundTemperatureService = null,
        ISolarRadiationService? solarRadiationService = null,
        IOptions<Iso52016EnergyNeedOptions>? energyNeedOptions = null,
        ILogger<EnergyCalculationPipelineService>? logger = null)
    {
        _rooms = rooms;
        _floors = floors;
        _buildings = buildings;
        _preferences = preferences;
        _roomLoadEngine = roomLoadEngine;
        _aggregationEngine = aggregationEngine;
        _annualEnergyEngine = annualEnergyEngine;
        _equipmentSizingCalculationUseCase = equipmentSizingCalculationUseCase;
        _systemEnergyHandoffUseCase = systemEnergyHandoffUseCase;
        _legacyEnergyCalculator = legacyEnergyCalculator;
        _coolingReferenceData = coolingReferenceData;
        _coolingOptions = coolingOptions.Value;
        _heatingOptions = heatingOptions.Value;
        _energyNeedOptions = energyNeedOptions?.Value ?? new Iso52016EnergyNeedOptions();
        _annualClimateDataProvider = annualClimateDataProvider;
        _groundTemperatureService = groundTemperatureService;
        _solarRadiationService = solarRadiationService;
        _logger = logger ?? NullLogger<EnergyCalculationPipelineService>.Instance;
        _climateContextBuilder = new EnergyCalculationPipelineClimateContextBuilder(
            _annualClimateDataProvider,
            _energyNeedOptions,
            _logger);
        _annualInputAdapter = new EnergyCalculationPipelineAnnualInputAdapter(timeProvider);
        _roomContextResolver = new EnergyCalculationPipelineRoomContextResolver(
            _coolingReferenceData,
            _groundTemperatureService,
            _solarRadiationService,
            _energyNeedOptions);
        _aggregationRoomAssembler = new EnergyCalculationPipelineAggregationRoomAssembler();
        _diagnosticsPolicy = new EnergyCalculationPipelineDiagnosticsPolicy();
        _buildingHeatingResultAssembler = new EnergyCalculationPipelineBuildingHeatingResultAssembler(_diagnosticsPolicy);
    }

    public async Task<Result<RoomLoadCalculationResult>> CalculateRoomLoadAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomLoadCalculationResult>.NotFound($"Room with id {roomId} not found.");

        var preferences = await GetPreferencesAsync(room.Floor.Building.ProjectId, cancellationToken);
        var climateContext = await _climateContextBuilder.BuildClimateContextAsync(room.Floor.Building, cancellationToken);
        return CalculateRoomLoad(room, preferences, climateContext);
    }

    public async Task<Result<RoomCalculationResult>> CalculateRoomCoolingLoadAsync(
        int roomId,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomCalculationResult>.NotFound($"Room with id {roomId} not found.");

        var preferences = await GetPreferencesAsync(room.Floor.Building.ProjectId, cancellationToken);
        var climateContext = await _climateContextBuilder.BuildClimateContextAsync(room.Floor.Building, cancellationToken);
        var load = CalculateRoomLoad(
            room,
            preferences,
            climateContext,
            requestedMethod: method.ToString());
        var roomFailure = _diagnosticsPolicy.TryMapRoomLoadFailureOrValidation<RoomCalculationResult>(load);
        if (roomFailure is not null)
            return roomFailure;

        return Result<RoomCalculationResult>.Success(
            EnergyCalculationPipelineResultMapper.MapCoolingRoomResult(
                room,
                load.Value,
                preferences,
                method,
                RoomPipelineMethod,
                ExternalReferenceValidationDesignPoint));
    }

    public async Task<Result<RoomHeatingLoadResult>> CalculateRoomHeatingLoadAsync(
        int roomId,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomHeatingLoadResult>.NotFound($"Room with id {roomId} not found.");

        var preferences = await GetPreferencesAsync(room.Floor.Building.ProjectId, cancellationToken);
        var climateContext = await _climateContextBuilder.BuildClimateContextAsync(room.Floor.Building, cancellationToken);
        var load = CalculateRoomLoad(
            room,
            preferences,
            climateContext,
            requestedMethod: method.ToString());
        var roomFailure = _diagnosticsPolicy.TryMapRoomLoadFailureOrValidation<RoomHeatingLoadResult>(load);
        if (roomFailure is not null)
            return roomFailure;

        return Result<RoomHeatingLoadResult>.Success(
            EnergyCalculationPipelineResultMapper.MapHeatingRoomResult(
                room,
                load.Value,
                method,
                preferences,
                RoomPipelineMethod,
                ExternalReferenceValidationDesignPoint));
    }

    public async Task<Result<FloorCalculationResult>> CalculateFloorLoadAsync(
        int floorId,
        string? requestedMethod = null,
        CancellationToken cancellationToken = default)
    {
        var floor = await _floors.GetForCalculationAsync(floorId, cancellationToken);
        if (floor is null)
            return Result<FloorCalculationResult>.NotFound($"Floor with id {floorId} not found.");

        var preferences = await GetPreferencesAsync(floor.Building.ProjectId, cancellationToken);
        var climateContext = await _climateContextBuilder.BuildClimateContextAsync(floor.Building, cancellationToken);
        var aggregation = AggregateFloor(
            floor,
            preferences,
            climateContext,
            requestedMethod);
        var aggregationFailure = _diagnosticsPolicy.TryMapAggregationFailureOrValidation<FloorCalculationResult>(aggregation);
        if (aggregationFailure is not null)
            return aggregationFailure;

        return Result<FloorCalculationResult>.Success(
            EnergyCalculationPipelineResultMapper.MapFloorResult(
                floor,
                aggregation.Value,
                preferences,
                requestedMethod,
                AggregationPipelineMethod,
                ExternalReferenceValidationDesignPoint));
    }

    public Task<Result<FloorCalculationResult>> CalculateFloorCoolingLoadAsync(
        int floorId,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default) =>
        CalculateFloorLoadAsync(floorId, method.ToString(), cancellationToken);

    public Task<Result<FloorCalculationResult>> CalculateFloorHeatingLoadAsync(
        int floorId,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CancellationToken cancellationToken = default) =>
        CalculateFloorLoadAsync(floorId, method.ToString(), cancellationToken);

    public async Task<Result<BuildingCalculationResult>> CalculateBuildingCoolingLoadAsync(
        int buildingId,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingCalculationResult>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await GetPreferencesAsync(building.ProjectId, cancellationToken);
        var climateContext = await _climateContextBuilder.BuildClimateContextAsync(building, cancellationToken);
        var aggregation = AggregateBuilding(
            building,
            preferences,
            climateContext,
            method.ToString());
        var aggregationFailure = _diagnosticsPolicy.TryMapAggregationFailureOrValidation<BuildingCalculationResult>(aggregation);
        if (aggregationFailure is not null)
            return aggregationFailure;

        return Result<BuildingCalculationResult>.Success(
            EnergyCalculationPipelineResultMapper.MapBuildingCoolingResult(
                building,
                aggregation.Value,
                preferences,
                method,
                AggregationPipelineMethod,
                ExternalReferenceValidationDesignPoint));
    }

    public async Task<Result<BuildingHeatingLoadResult>> CalculateBuildingHeatingLoadAsync(
        int buildingId,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingHeatingLoadResult>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await GetPreferencesAsync(building.ProjectId, cancellationToken);
        var climateContext = await _climateContextBuilder.BuildClimateContextAsync(building, cancellationToken);
        var aggregation = AggregateBuilding(
            building,
            preferences,
            climateContext,
            method.ToString());
        var aggregationFailure = _diagnosticsPolicy.TryMapAggregationFailureOrValidation<BuildingHeatingLoadResult>(aggregation);
        if (aggregationFailure is not null)
            return aggregationFailure;

        var roomResults = _buildingHeatingResultAssembler.BuildRoomHeatingResults(
            building,
            preferences,
            method,
            RoomPipelineMethod,
            ExternalReferenceValidationDesignPoint,
            (room, requestedMethod) => CalculateRoomLoad(
                room,
                preferences,
                climateContext,
                requestedMethod));
        if (roomResults.IsFailure)
            return Result<BuildingHeatingLoadResult>.Failure(roomResults);

        return Result<BuildingHeatingLoadResult>.Success(
            EnergyCalculationPipelineResultMapper.MapBuildingHeatingResult(
                building,
                aggregation.Value,
                roomResults.Value,
                method,
                AggregationPipelineMethod,
                ExternalReferenceValidationDesignPoint));
    }

    public async Task<Result<BuildingEnergyBalanceResult>> CalculateBuildingEnergyBalanceAsync(
        int buildingId,
        CoolingLoadCalculationMethod coolingMethod = CoolingLoadCalculationMethod.Iso52016,
        HeatingLoadCalculationMethod heatingMethod = HeatingLoadCalculationMethod.En12831,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingEnergyBalanceResult>.NotFound($"Building with id {buildingId} not found.");

        if (building.ClimateZone is null)
            return Result<BuildingEnergyBalanceResult>.Validation("Building climate zone is required for Standard-Based Calculation energy balance.");

        var preferences = await GetPreferencesAsync(building.ProjectId, cancellationToken);
        var source = await _legacyEnergyCalculator.CalculateAsync(
            building,
            coolingMethod,
            heatingMethod,
            preferences,
            cancellationToken);
        if (source.HourlyBalanceRecords.Count == 0 && source.MonthlyBalances.Count == 0)
        {
            return Result<BuildingEnergyBalanceResult>.Validation(
                "Annual energy balance source is unavailable: neither true hourly simulation records nor monthly balances were available.");
        }

        var annualInput = _annualInputAdapter.BuildAnnualEnergyInput(building, source);
        var annual = _annualEnergyEngine.Calculate(annualInput.Input);
        if (annual.IsFailure)
            return Result<BuildingEnergyBalanceResult>.Failure(annual);

        var result = EnergyCalculationPipelineResultMapper.MapEnergyBalanceResult(
            source,
            annual.Value,
            coolingMethod,
            heatingMethod,
            annualInput.Source,
            annualInput.IsTrueHourly8760,
            annualInput.HourlyRecordCount,
            annualInput.Diagnostics,
            ExternalReferenceValidationAnnualAggregationAdapter);

        return Result<BuildingEnergyBalanceResult>.Success(result);
    }

    public async Task<Result<SystemEnergyHandoffResult>> CalculateBuildingSystemEnergyFromUsefulDemandAsync(
        int buildingId,
        CoolingLoadCalculationMethod coolingMethod = CoolingLoadCalculationMethod.Iso52016,
        HeatingLoadCalculationMethod heatingMethod = HeatingLoadCalculationMethod.En12831,
        DomesticHotWaterEn15316Handoff? dhwHandoff = null,
        CancellationToken cancellationToken = default) =>
        await _systemEnergyHandoffUseCase.CalculateBuildingSystemEnergyFromUsefulDemandAsync(
            buildingId,
            coolingMethod,
            heatingMethod,
            dhwHandoff,
            cancellationToken);

    public async Task<Result<EquipmentSizingResult>> CalculateRoomEquipmentSizingAsync(
        int roomId,
        string systemType,
        string unitType,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default) =>
        await _equipmentSizingCalculationUseCase.CalculateRoomEquipmentSizingAsync(
            roomId,
            systemType,
            unitType,
            method,
            cancellationToken);

    private async Task<CalculationPreferences> GetPreferencesAsync(
        int projectId,
        CancellationToken cancellationToken) =>
        await _preferences.GetByProjectIdAsync(projectId, cancellationToken) ??
        CalculationPreferences.Default();
}
