using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
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
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

public sealed class EnergyCalculationPipelineService
{
    private const string RoomPipelineMethod = "Energy Calculation Parity / Application Room Load Pipeline";
    private const string AggregationPipelineMethod = "Energy Calculation Parity / Application Load Aggregation Pipeline";
    private const string EnergyCalculationParityDesignPoint = "EnergyCalculationParityDesignPoint";
    private const string EnergyCalculationParityAnnualAggregationAdapter = "EnergyCalculationParityAnnualAggregationAdapter";
    private readonly IRoomRepository _rooms;
    private readonly IFloorRepository _floors;
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly RoomLoadCalculationEngine _roomLoadEngine;
    private readonly LoadAggregationEngine _aggregationEngine;
    private readonly AnnualEnergyBalanceEngine _annualEnergyEngine;
    private readonly EquipmentSizingEngine _equipmentSizingEngine;
    private readonly IBuildingEnergyCalculator _legacyEnergyCalculator;
    private readonly ICoolingLoadReferenceData _coolingReferenceData;
    private readonly ICoolingEquipmentCatalogSizingProvider? _equipmentCatalogSizingProvider;
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

    public EnergyCalculationPipelineService(
        IRoomRepository rooms,
        IFloorRepository floors,
        IBuildingRepository buildings,
        ICalculationPreferencesRepository preferences,
        RoomLoadCalculationEngine roomLoadEngine,
        LoadAggregationEngine aggregationEngine,
        AnnualEnergyBalanceEngine annualEnergyEngine,
        EquipmentSizingEngine equipmentSizingEngine,
        IBuildingEnergyCalculator legacyEnergyCalculator,
        ICoolingLoadReferenceData coolingReferenceData,
        IOptions<CoolingLoadCalculationOptions> coolingOptions,
        IOptions<En12831HeatingLoadOptions> heatingOptions,
        TimeProvider timeProvider,
        ICoolingEquipmentCatalogSizingProvider? equipmentCatalogSizingProvider = null,
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
        _equipmentSizingEngine = equipmentSizingEngine;
        _legacyEnergyCalculator = legacyEnergyCalculator;
        _coolingReferenceData = coolingReferenceData;
        _coolingOptions = coolingOptions.Value;
        _heatingOptions = heatingOptions.Value;
        _energyNeedOptions = energyNeedOptions?.Value ?? new Iso52016EnergyNeedOptions();
        _equipmentCatalogSizingProvider = equipmentCatalogSizingProvider;
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
        if (load.IsFailure)
            return Result<RoomCalculationResult>.Failure(load);

        if (load.Value.HasErrors)
            return Result<RoomCalculationResult>.Validation(FormatErrorDiagnostics(load.Value.Diagnostics));

        return Result<RoomCalculationResult>.Success(
            EnergyCalculationPipelineResultMapper.MapCoolingRoomResult(
                room,
                load.Value,
                preferences,
                method,
                RoomPipelineMethod,
                EnergyCalculationParityDesignPoint));
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
        if (load.IsFailure)
            return Result<RoomHeatingLoadResult>.Failure(load);

        if (load.Value.HasErrors)
            return Result<RoomHeatingLoadResult>.Validation(FormatErrorDiagnostics(load.Value.Diagnostics));

        return Result<RoomHeatingLoadResult>.Success(
            EnergyCalculationPipelineResultMapper.MapHeatingRoomResult(
                room,
                load.Value,
                method,
                preferences,
                RoomPipelineMethod,
                EnergyCalculationParityDesignPoint));
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
        if (aggregation.IsFailure)
            return Result<FloorCalculationResult>.Failure(aggregation);

        if (aggregation.Value.HasErrors)
            return Result<FloorCalculationResult>.Validation(FormatErrorDiagnostics(aggregation.Value.Diagnostics));

        return Result<FloorCalculationResult>.Success(
            EnergyCalculationPipelineResultMapper.MapFloorResult(
                floor,
                aggregation.Value,
                preferences,
                requestedMethod,
                AggregationPipelineMethod,
                EnergyCalculationParityDesignPoint));
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
        if (aggregation.IsFailure)
            return Result<BuildingCalculationResult>.Failure(aggregation);

        if (aggregation.Value.HasErrors)
            return Result<BuildingCalculationResult>.Validation(FormatErrorDiagnostics(aggregation.Value.Diagnostics));

        return Result<BuildingCalculationResult>.Success(
            EnergyCalculationPipelineResultMapper.MapBuildingCoolingResult(
                building,
                aggregation.Value,
                preferences,
                method,
                AggregationPipelineMethod,
                EnergyCalculationParityDesignPoint));
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
        if (aggregation.IsFailure)
            return Result<BuildingHeatingLoadResult>.Failure(aggregation);

        if (aggregation.Value.HasErrors)
            return Result<BuildingHeatingLoadResult>.Validation(FormatErrorDiagnostics(aggregation.Value.Diagnostics));

        var roomResults = new List<RoomHeatingLoadResult>();
        foreach (var room in building.Floors.SelectMany(floor => floor.Rooms).OrderBy(room => room.Id))
        {
            var roomLoad = CalculateRoomLoad(
                room,
                preferences,
                climateContext,
                requestedMethod: method.ToString());
            if (roomLoad.IsFailure)
                return Result<BuildingHeatingLoadResult>.Failure(roomLoad);

            if (roomLoad.Value.HasErrors)
                return Result<BuildingHeatingLoadResult>.Validation(FormatErrorDiagnostics(roomLoad.Value.Diagnostics));

            roomResults.Add(
                EnergyCalculationPipelineResultMapper.MapHeatingRoomResult(
                    room,
                    roomLoad.Value,
                    method,
                    preferences,
                    RoomPipelineMethod,
                    EnergyCalculationParityDesignPoint));
        }

        return Result<BuildingHeatingLoadResult>.Success(
            EnergyCalculationPipelineResultMapper.MapBuildingHeatingResult(
                building,
                aggregation.Value,
                roomResults,
                method,
                AggregationPipelineMethod,
                EnergyCalculationParityDesignPoint));
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
            return Result<BuildingEnergyBalanceResult>.Validation("Building climate zone is required for Energy Calculation Parity energy balance.");

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
            EnergyCalculationParityAnnualAggregationAdapter);

        return Result<BuildingEnergyBalanceResult>.Success(result);
    }

    public async Task<Result<EquipmentSizingResult>> CalculateRoomEquipmentSizingAsync(
        int roomId,
        string systemType,
        string unitType,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(systemType))
            return Result<EquipmentSizingResult>.Validation("System type is required.");

        if (string.IsNullOrWhiteSpace(unitType))
            return Result<EquipmentSizingResult>.Validation("Unit type is required.");

        if (_equipmentCatalogSizingProvider is null)
            return Result<EquipmentSizingResult>.Validation("Equipment catalog sizing provider is not configured.");

        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<EquipmentSizingResult>.NotFound($"Room with id {roomId} not found.");

        var preferences = await GetPreferencesAsync(room.Floor.Building.ProjectId, cancellationToken);
        var climateContext = await _climateContextBuilder.BuildClimateContextAsync(room.Floor.Building, cancellationToken);
        var load = CalculateRoomLoad(
            room,
            preferences,
            climateContext,
            requestedMethod: method.ToString());
        if (load.IsFailure)
            return Result<EquipmentSizingResult>.Failure(load);

        if (load.Value.HasErrors)
            return Result<EquipmentSizingResult>.Validation(FormatErrorDiagnostics(load.Value.Diagnostics));

        var catalog = await _equipmentCatalogSizingProvider.ListActiveCoolingCandidatesAsync(
            systemType,
            unitType,
            cancellationToken);
        var candidates = catalog
            .Select(candidate => new EquipmentSizingCandidateInput(
                candidate.CatalogItemId,
                candidate.Manufacturer,
                candidate.ModelName,
                $"{candidate.SystemType}/{candidate.UnitType}",
                HeatingCapacityW: candidate.NominalHeatingCapacityKw * 1000,
                CoolingCapacityW: candidate.NominalCoolingCapacityKw * 1000,
                IsActive: true))
            .ToArray();
        var canEvaluateHeating = load.Value.HeatingLoadW > 0 &&
            catalog.Any(candidate => candidate.NominalHeatingCapacityKw.HasValue);
        var evaluatedHeatingLoadW = canEvaluateHeating ? load.Value.HeatingLoadW : 0;

        var sizing = _equipmentSizingEngine.Calculate(new EquipmentSizingInput(
            TargetId: room.Id,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: evaluatedHeatingLoadW,
            RequiredCoolingLoadW: load.Value.CoolingLoadW,
            SafetyFactor: preferences.CoolingSafetyFactor,
            Candidates: candidates,
            DiagnosticsContext: $"Room {room.Id} equipment selection",
            HeatingSafetyFactor: preferences.HeatingSafetyFactor,
            CoolingSafetyFactor: preferences.CoolingSafetyFactor));

        if (sizing.IsFailure)
            return sizing;

        var diagnostics = sizing.Value.Diagnostics.ToList();
        if (load.Value.HeatingLoadW > 0 && !canEvaluateHeating)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "EquipmentSizing.HeatingCapacityUnavailable",
                "Heating sizing is skipped because catalog items do not expose heating capacity.",
                $"Room {room.Id} equipment selection"));
        }

        return Result<EquipmentSizingResult>.Success(sizing.Value with
        {
            RequiredHeatingCapacityW = Round(load.Value.HeatingLoadW),
            RequiredHeatingCapacityWithReserveW = Round(load.Value.HeatingLoadW * preferences.HeatingSafetyFactor),
            Diagnostics = diagnostics
        });
    }

    private Result<RoomLoadCalculationResult> CalculateRoomLoad(
        Room room,
        CalculationPreferences preferences,
        PipelineClimateContext climateContext,
        string? requestedMethod = null)
    {
        if (room.Floor.Building.ClimateZone is null)
            return Result<RoomLoadCalculationResult>.Validation("Building climate zone is required for Energy Calculation Parity room load calculation.");

        var input = BuildRoomLoadInput(
            room,
            preferences,
            climateContext,
            requestedMethod);
        var result = _roomLoadEngine.Calculate(input);
        if (result.IsFailure)
            return result;

        _logger.LogDebug(
            "Energy Calculation Parity room load calculated for room {RoomId}: heating {HeatingLoadW} W, cooling {CoolingLoadW} W.",
            room.Id,
            result.Value.HeatingLoadW,
            result.Value.CoolingLoadW);

        return result;
    }

    private RoomLoadCalculationInput BuildRoomLoadInput(
        Room room,
        CalculationPreferences preferences,
        PipelineClimateContext climateContext,
        string? requestedMethod)
    {
        var indoor = room.IndoorTemperature.Celsius;
        var heatingOutdoor = room.Floor.Building.ClimateZone?.WinterDesignTemperature.Celsius ??
            room.OutdoorTemperatureOverride?.Celsius ??
            _heatingOptions.DefaultOutdoorHeatingDesignTemperatureC;
        var coolingOutdoor = room.OutdoorTemperatureOverride?.Celsius ??
            room.Floor.Building.ClimateZone?.SummerDesignTemperature.Celsius ??
            _coolingOptions.DefaultOutdoorCoolingDesignTemperatureC;
        var diagnostics = new List<CalculationDiagnostic>();
        var assumptions = new List<string>();
        EnergyCalculationPipelineResultMapper.AddMethodCompatibilityDiagnostic(
            diagnostics,
            requestedMethod,
            $"Room {room.Id} application load pipeline",
            "Energy Calculation Parity design-point pipeline");
        EnergyCalculationPipelineRoomContextResolver.AddInternalGainScheduleDiagnostics(room, diagnostics, assumptions);

        var groundContext = _roomContextResolver.ResolveGroundContext(room, climateContext);
        diagnostics.AddRange(groundContext.Diagnostics);
        assumptions.AddRange(groundContext.Assumptions);
        var heatingTransmission = RoomTransmissionInputFactory.CreateForRoom(
            room,
            indoor,
            heatingOutdoor,
            groundContext.HeatingGroundTemperatureC).Elements;
        var coolingTransmission = RoomTransmissionInputFactory.CreateForRoom(
            room,
            indoor,
            coolingOutdoor,
            groundContext.CoolingGroundTemperatureC).Elements;

        var solarContext = _roomContextResolver.ResolveSolarContext(room, climateContext);
        diagnostics.AddRange(solarContext.Diagnostics);
        assumptions.AddRange(solarContext.Assumptions);

        return new RoomLoadCalculationInput(
            RoomId: room.Id,
            RoomCode: null,
            RoomName: room.Name,
            AreaM2: room.Area.SquareMeters,
            VolumeM3: room.CalculateVolume(),
            HeatingSetpointC: indoor,
            CoolingSetpointC: indoor,
            OutdoorDesignHeatingTemperatureC: heatingOutdoor,
            OutdoorDesignCoolingTemperatureC: coolingOutdoor,
            TransmissionElements: heatingTransmission,
            CoolingTransmissionElements: coolingTransmission,
            WindowSolarGains: CreateSolarInput(room, solarContext.IrradianceByWindowId),
            HeatingVentilationAndInfiltration: CreateVentilationInput(
                room,
                preferences,
                indoor,
                heatingOutdoor,
                isHeating: true,
                diagnostics),
            CoolingVentilationAndInfiltration: CreateVentilationInput(
                room,
                preferences,
                indoor,
                coolingOutdoor,
                isHeating: false,
                diagnostics),
            InternalGains: CreateInternalGainInput(room),
            ApplicationDiagnostics: diagnostics,
            ApplicationAssumptions: assumptions,
            DiagnosticsContext: $"Room {room.Id} application load pipeline");
    }

    private RoomWindowSolarGainRequest? CreateSolarInput(
        Room room,
        IReadOnlyDictionary<int, double> irradianceByWindowId)
    {
        if (room.Windows.Count == 0)
            return null;

        var windows = room.Windows
            .Select(window => WindowSolarGainInputFactory.CreateForWindow(
                window,
                irradianceByWindowId.GetValueOrDefault(
                    window.Id,
                    _coolingReferenceData.GetWindowSolarLoadWPerM2(window.Orientation)),
                diagnosticsContext: $"Room {room.Id} window {window.Id} application solar gain"))
            .ToArray();

        return new RoomWindowSolarGainRequest(room.Id, windows);
    }

    private VentilationAndInfiltrationLoadInput CreateVentilationInput(
        Room room,
        CalculationPreferences preferences,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        bool isHeating,
        List<CalculationDiagnostic> diagnostics)
    {
        var deltaT = isHeating
            ? Math.Max(indoorTemperatureC - outdoorTemperatureC, 0)
            : Math.Max(outdoorTemperatureC - indoorTemperatureC, 0);
        var ventilation = room.VentilationParameters;
        var defaultAch = preferences.Iso52016DefaultAirChangesPerHour;
        var effectiveVentilation = EnergyCalculationPipelineResultMapper.ResolveEffectiveVentilationAssumption(
            room,
            preferences,
            deltaT);
        if (ventilation is null)
        {
            diagnostics.Add(new CalculationDiagnostic(
                defaultAch < 0
                    ? CalculationDiagnosticSeverity.Error
                    : CalculationDiagnosticSeverity.Warning,
                defaultAch < 0
                    ? "Ventilation.InvalidDefaultAirChangesPerHour"
                    : "Ventilation.DefaultAirChangesPerHourUsed",
                defaultAch < 0
                    ? "Room ventilation parameters are missing and the default ACH is invalid."
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "Room ventilation parameters are missing; default ACH {0} was used. Effective mechanical airflow {1} m3/h; effective infiltration ACH {2}; effective infiltration airflow {3} m3/h.",
                        Round(defaultAch),
                        Round(effectiveVentilation.EffectiveMechanicalAirflowM3PerHour),
                        Round(effectiveVentilation.EffectiveInfiltrationAirChangesPerHour),
                        Round(effectiveVentilation.EffectiveInfiltrationAirflowM3PerHour)),
                $"Room {room.Id} application {(isHeating ? "heating" : "cooling")} ventilation"));
        }
        else
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "Ventilation.RoomParametersUsed",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Room ventilation parameters were used. Effective ACH {0}; effective mechanical airflow {1} m3/h; effective infiltration ACH {2}; effective infiltration airflow {3} m3/h.",
                    Round(effectiveVentilation.EffectiveAirChangesPerHour),
                    Round(effectiveVentilation.EffectiveMechanicalAirflowM3PerHour),
                    Round(effectiveVentilation.EffectiveInfiltrationAirChangesPerHour),
                    Round(effectiveVentilation.EffectiveInfiltrationAirflowM3PerHour)),
                $"Room {room.Id} application {(isHeating ? "heating" : "cooling")} ventilation"));
        }

        return new VentilationAndInfiltrationLoadInput(
            RoomId: room.Id,
            AreaM2: room.Area.SquareMeters,
            VolumeM3: room.CalculateVolume(),
            OccupancyPeople: room.PeopleCount,
            IndoorTemperatureC: indoorTemperatureC,
            OutdoorTemperatureC: outdoorTemperatureC,
            AirChangesPerHour: effectiveVentilation.EffectiveAirChangesPerHour,
            InfiltrationAirChangesPerHour: effectiveVentilation.EffectiveInfiltrationAirChangesPerHour,
            HeatRecoveryEfficiency: ventilation?.HeatRecoveryEfficiency ?? 0,
            AirDensityKgPerM3: AirPhysicalConstants.AirDensityKgPerM3,
            AirSpecificHeatJPerKgK: AirPhysicalConstants.AirSpecificHeatJPerKgK,
            DiagnosticsContext: $"Room {room.Id} application {(isHeating ? "heating" : "cooling")} ventilation");
    }

    private InternalGainInput CreateInternalGainInput(Room room) =>
        new(
            RoomId: room.Id,
            AreaM2: room.Area.SquareMeters,
            OccupancyPeople: room.PeopleCount,
            SensibleGainPerPersonW: _coolingReferenceData.GetPeopleHeatGainW(room.Type),
            EquipmentLoadW: room.EquipmentLoad.Watts,
            LightingLoadW: room.LightingLoad.Watts,
            DiagnosticsContext: $"Room {room.Id} application internal gains");

    private Result<LoadAggregationResult> AggregateFloor(
        Floor floor,
        CalculationPreferences preferences,
        PipelineClimateContext climateContext,
        string? requestedMethod)
    {
        var rooms = _aggregationRoomAssembler.BuildAggregationRooms(
            floor.Rooms,
            floor.Building,
            room => CalculateRoomLoad(
                room,
                preferences,
                climateContext,
                requestedMethod));
        if (rooms.IsFailure)
            return Result<LoadAggregationResult>.Failure(rooms);

        return _aggregationEngine.Aggregate(new LoadAggregationInput(
            floor.Id,
            LoadAggregationTargetType.Floor,
            rooms.Value,
            LoadAggregationMode.DesignPoint,
            floor.Name,
            $"Floor {floor.Id} application aggregation"));
    }

    private Result<LoadAggregationResult> AggregateBuilding(
        Building building,
        CalculationPreferences preferences,
        PipelineClimateContext climateContext,
        string? requestedMethod)
    {
        var rooms = _aggregationRoomAssembler.BuildAggregationRooms(
            building.Floors.SelectMany(floor => floor.Rooms),
            building,
            room => CalculateRoomLoad(
                room,
                preferences,
                climateContext,
                requestedMethod));
        if (rooms.IsFailure)
            return Result<LoadAggregationResult>.Failure(rooms);

        return _aggregationEngine.Aggregate(new LoadAggregationInput(
            building.Id,
            LoadAggregationTargetType.Building,
            rooms.Value,
            LoadAggregationMode.DesignPoint,
            building.Name,
            $"Building {building.Id} application aggregation"));
    }

    private async Task<CalculationPreferences> GetPreferencesAsync(
        int projectId,
        CancellationToken cancellationToken) =>
        await _preferences.GetByProjectIdAsync(projectId, cancellationToken) ??
        CalculationPreferences.Default();

    private static string FormatErrorDiagnostics(IEnumerable<CalculationDiagnostic> diagnostics) =>
        string.Join("; ", diagnostics
            .Where(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error)
            .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
