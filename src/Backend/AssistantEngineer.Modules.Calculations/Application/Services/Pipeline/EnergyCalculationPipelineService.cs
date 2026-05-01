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
    private const string TrueHourlySimulationSource = HourlySimulationToAnnualEnergyInputMapper.TrueHourlySimulationSource;
    private const string MonthlyBalanceAdapterSource = "MonthlyBalanceAdapter";
    private const string AnnualClimateDataSolarSource = "AnnualClimateData";
    private const string ReferenceSolarFallbackSource = "ReferenceByOrientationFallback";

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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EnergyCalculationPipelineService> _logger;

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
        _timeProvider = timeProvider;
        _equipmentCatalogSizingProvider = equipmentCatalogSizingProvider;
        _annualClimateDataProvider = annualClimateDataProvider;
        _groundTemperatureService = groundTemperatureService;
        _solarRadiationService = solarRadiationService;
        _logger = logger ?? NullLogger<EnergyCalculationPipelineService>.Instance;
    }

    public async Task<Result<RoomLoadCalculationResult>> CalculateRoomLoadAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomLoadCalculationResult>.NotFound($"Room with id {roomId} not found.");

        var preferences = await GetPreferencesAsync(room.Floor.Building.ProjectId, cancellationToken);
        var climateContext = await BuildClimateContextAsync(room.Floor.Building, cancellationToken);
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
        var climateContext = await BuildClimateContextAsync(room.Floor.Building, cancellationToken);
        var load = CalculateRoomLoad(
            room,
            preferences,
            climateContext,
            requestedMethod: method.ToString());
        if (load.IsFailure)
            return Result<RoomCalculationResult>.Failure(load);

        if (load.Value.HasErrors)
            return Result<RoomCalculationResult>.Validation(FormatErrorDiagnostics(load.Value.Diagnostics));

        return Result<RoomCalculationResult>.Success(MapCoolingRoomResult(room, load.Value, preferences, method));
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
        var climateContext = await BuildClimateContextAsync(room.Floor.Building, cancellationToken);
        var load = CalculateRoomLoad(
            room,
            preferences,
            climateContext,
            requestedMethod: method.ToString());
        if (load.IsFailure)
            return Result<RoomHeatingLoadResult>.Failure(load);

        if (load.Value.HasErrors)
            return Result<RoomHeatingLoadResult>.Validation(FormatErrorDiagnostics(load.Value.Diagnostics));

        return Result<RoomHeatingLoadResult>.Success(MapHeatingRoomResult(room, load.Value, method, preferences));
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
        var climateContext = await BuildClimateContextAsync(floor.Building, cancellationToken);
        var aggregation = AggregateFloor(
            floor,
            preferences,
            climateContext,
            requestedMethod);
        if (aggregation.IsFailure)
            return Result<FloorCalculationResult>.Failure(aggregation);

        if (aggregation.Value.HasErrors)
            return Result<FloorCalculationResult>.Validation(FormatErrorDiagnostics(aggregation.Value.Diagnostics));

        return Result<FloorCalculationResult>.Success(MapFloorResult(
            floor,
            aggregation.Value,
            preferences,
            requestedMethod));
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
        var climateContext = await BuildClimateContextAsync(building, cancellationToken);
        var aggregation = AggregateBuilding(
            building,
            preferences,
            climateContext,
            method.ToString());
        if (aggregation.IsFailure)
            return Result<BuildingCalculationResult>.Failure(aggregation);

        if (aggregation.Value.HasErrors)
            return Result<BuildingCalculationResult>.Validation(FormatErrorDiagnostics(aggregation.Value.Diagnostics));

        return Result<BuildingCalculationResult>.Success(MapBuildingCoolingResult(building, aggregation.Value, preferences, method));
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
        var climateContext = await BuildClimateContextAsync(building, cancellationToken);
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

            roomResults.Add(MapHeatingRoomResult(room, roomLoad.Value, method, preferences));
        }

        return Result<BuildingHeatingLoadResult>.Success(MapBuildingHeatingResult(
            building,
            aggregation.Value,
            roomResults,
            method));
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

        var annualInput = BuildAnnualEnergyInput(building, source);
        var annual = _annualEnergyEngine.Calculate(annualInput.Input);
        if (annual.IsFailure)
            return Result<BuildingEnergyBalanceResult>.Failure(annual);

        var result = MapEnergyBalanceResult(
            source,
            annual.Value,
            coolingMethod,
            heatingMethod,
            annualInput.Source,
            annualInput.IsTrueHourly8760,
            annualInput.HourlyRecordCount,
            annualInput.Diagnostics);

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
        var climateContext = await BuildClimateContextAsync(room.Floor.Building, cancellationToken);
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
        AddMethodCompatibilityDiagnostic(diagnostics, requestedMethod, $"Room {room.Id} application load pipeline");
        AddInternalGainScheduleDiagnostics(room, diagnostics, assumptions);

        var groundContext = ResolveGroundContext(room, climateContext);
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

        var solarContext = ResolveSolarContext(room, climateContext);
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
        var effectiveVentilation = ResolveEffectiveVentilationAssumption(room, preferences, deltaT);
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

    private static EffectiveVentilationAssumption ResolveEffectiveVentilationAssumption(
        Room room,
        CalculationPreferences preferences,
        double deltaT)
    {
        var ventilation = room.VentilationParameters;
        var volumeM3 = room.CalculateVolume();
        var mechanicalAch = ventilation?.AirChangesPerHour ??
            preferences.Iso52016DefaultAirChangesPerHour;
        var infiltrationAch = ventilation is null
            ? 0
            : ventilation.InfiltrationAirChangesPerHour + ventilation.StackCoefficient * Math.Sqrt(deltaT);
        var source = ventilation is null
            ? "DefaultCalculationPreferences"
            : "RoomVentilationParameters";

        return new EffectiveVentilationAssumption(
            EffectiveAirChangesPerHour: mechanicalAch,
            EffectiveMechanicalAirflowM3PerHour: mechanicalAch * volumeM3,
            EffectiveInfiltrationAirChangesPerHour: infiltrationAch,
            EffectiveInfiltrationAirflowM3PerHour: infiltrationAch * volumeM3,
            Source: source);
    }

    private async Task<PipelineClimateContext> BuildClimateContextAsync(
        Building building,
        CancellationToken cancellationToken)
    {
        if (_annualClimateDataProvider is null ||
            building.ClimateZone is null)
        {
            return new PipelineClimateContext(null, IsCompleteAnnualClimateData: false);
        }

        AnnualClimateData? annualData;
        try
        {
            annualData = await _annualClimateDataProvider.GetForClimateZoneAsync(
                building.ClimateZone.Id,
                _energyNeedOptions.DefaultWeatherYear,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Annual climate data was unavailable for building {BuildingId}; Energy Calculation Parity pipeline will use documented design-point fallbacks.",
                building.Id);
            return new PipelineClimateContext(null, IsCompleteAnnualClimateData: false);
        }

        return new PipelineClimateContext(
            annualData,
            IsCompleteAnnualClimateData: HasCompleteAnnualClimateData(annualData));
    }

    private RoomGroundContext ResolveGroundContext(
        Room room,
        PipelineClimateContext climateContext)
    {
        if (!room.Walls.Any(wall => wall.BoundaryType == WallBoundaryType.Ground))
            return RoomGroundContext.Empty;

        var diagnostics = new List<CalculationDiagnostic>();
        var assumptions = new List<string>();
        var context = $"Room {room.Id} application ground boundary";

        if (room.GroundContactMetadata is null)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "GroundContact.MetadataMissing",
                "Ground boundary exists, but ground contact metadata is missing.",
                context));
        }

        if (_groundTemperatureService is not null &&
            climateContext.AnnualClimateData is not null &&
            climateContext.AnnualClimateData.HourlyData.Count > 0)
        {
            var monthly = Enumerable.Range(1, 12)
                .Select(month => _groundTemperatureService.GetMonthlyAverageTemperature(
                    climateContext.AnnualClimateData.HourlyData.ToArray(),
                    month))
                .ToArray();

            var heatingGroundTemperature = monthly.Min();
            var coolingGroundTemperature = monthly.Max();
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "GroundContact.GroundTemperatureProfileUsed",
                "Ground boundary temperature was resolved from the existing ground temperature profile service.",
                context));
            assumptions.Add("Ground boundary uses existing ground temperature profile values for design-point transmission.");
            return new RoomGroundContext(
                heatingGroundTemperature,
                coolingGroundTemperature,
                diagnostics,
                assumptions);
        }

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "GroundContact.DefaultBoundaryTemperatureUsed",
            $"Ground boundary temperature profile was unavailable; default boundary temperature {_energyNeedOptions.DefaultGroundBoundaryTemperatureC} C was used.",
            context));
        assumptions.Add("Ground boundary uses the configured default boundary temperature when profile data is unavailable.");
        return new RoomGroundContext(
            _energyNeedOptions.DefaultGroundBoundaryTemperatureC,
            _energyNeedOptions.DefaultGroundBoundaryTemperatureC,
            diagnostics,
            assumptions);
    }

    private RoomSolarContext ResolveSolarContext(
        Room room,
        PipelineClimateContext climateContext)
    {
        if (room.Windows.Count == 0)
            return RoomSolarContext.Empty;

        var diagnostics = new List<CalculationDiagnostic>();
        var assumptions = new List<string>();
        var irradianceByWindowId = new Dictionary<int, double>();
        var context = $"Room {room.Id} application solar gains";

        if (_solarRadiationService is not null &&
            climateContext.AnnualClimateData is not null &&
            climateContext.AnnualClimateData.HourlyData.Count > 0)
        {
            foreach (var window in room.Windows)
            {
                var irradiance = climateContext.AnnualClimateData.HourlyData
                    .Select(hour =>
                    {
                        var timestamp = new DateTime(
                                climateContext.AnnualClimateData.Year,
                                1,
                                1,
                                0,
                                0,
                                0,
                                DateTimeKind.Utc)
                            .AddHours(hour.HourOfYear);
                        return _solarRadiationService.CalculateVerticalSurfaceRadiation(
                            hour,
                            window.Orientation,
                            _energyNeedOptions.LatitudeDegrees,
                            timestamp.DayOfYear,
                            timestamp.Hour);
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                irradianceByWindowId[window.Id] = irradiance;
            }

            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "SolarGains.IrradianceSource",
                $"Solar irradiance source: {AnnualClimateDataSolarSource}.",
                context));
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "SolarWeather.AnnualClimateSolarDataUsed",
                "Annual climate direct and diffuse solar data were used to resolve design-point window irradiance.",
                context));
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "SolarWeather.SurfaceIrradianceCalculated",
                "Window irradiance was calculated through the centralized solar position and surface irradiance path.",
                context));
            assumptions.Add("Window solar gains use available annual climate solar data for design-point irradiance.");
            return new RoomSolarContext(irradianceByWindowId, diagnostics, assumptions);
        }

        foreach (var window in room.Windows)
        {
            irradianceByWindowId[window.Id] =
                _coolingReferenceData.GetWindowSolarLoadWPerM2(window.Orientation);
        }

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "SolarGains.ReferenceByOrientationFallback",
            "Window solar gain uses orientation reference irradiance because hourly weather/solar context was not available.",
            context));
        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "SolarWeather.ReferenceByOrientationFallbackUsed",
            "Reference irradiance by window orientation was used because hourly weather/solar context was not available.",
            context));
        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "SolarWeather.MissingDirectDiffuseSolarData",
            "Hourly direct and diffuse solar data were unavailable for this application path.",
            context));
        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "SolarGains.IrradianceSource",
            $"Solar irradiance source: {ReferenceSolarFallbackSource}.",
            context));
        assumptions.Add("Window solar gains use orientation reference irradiance fallback when annual weather/solar data is unavailable.");
        return new RoomSolarContext(irradianceByWindowId, diagnostics, assumptions);
    }

    private static void AddMethodCompatibilityDiagnostic(
        List<CalculationDiagnostic> diagnostics,
        string? requestedMethod,
        string context,
        string actualMethodLabel = "Energy Calculation Parity design-point pipeline")
    {
        if (string.IsNullOrWhiteSpace(requestedMethod))
            return;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "CalculationMethod.ApiCompatibility",
            $"Requested method '{requestedMethod}' is accepted for API compatibility, but this endpoint currently uses {actualMethodLabel}.",
            context));
    }

    private static void AddInternalGainScheduleDiagnostics(
        Room room,
        List<CalculationDiagnostic> diagnostics,
        List<string> assumptions)
    {
        var context = $"Room {room.Id} application internal gains";
        var hasSchedules =
            room.OccupancySchedule is not null ||
            room.EquipmentSchedule is not null ||
            room.LightingSchedule is not null;

        diagnostics.Add(new CalculationDiagnostic(
            hasSchedules ? CalculationDiagnosticSeverity.Warning : CalculationDiagnosticSeverity.Info,
            hasSchedules
                ? "InternalGains.DesignPointFullScheduleFactorWithSchedules"
                : "InternalGains.DesignPointFullScheduleFactor",
            hasSchedules
                ? "Design-point internal gains use full schedule factor 1.0; room schedules are reserved for hourly analysis paths."
                : "Design-point internal gains use full schedule factor 1.0.",
            context));
        assumptions.Add("Design-point internal gains use full schedule factor 1.0.");
    }

    private Result<LoadAggregationResult> AggregateFloor(
        Floor floor,
        CalculationPreferences preferences,
        PipelineClimateContext climateContext,
        string? requestedMethod)
    {
        var rooms = BuildAggregationRooms(
            floor.Rooms,
            floor.Building,
            preferences,
            climateContext,
            requestedMethod);
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
        var rooms = BuildAggregationRooms(
            building.Floors.SelectMany(floor => floor.Rooms),
            building,
            preferences,
            climateContext,
            requestedMethod);
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

    private Result<IReadOnlyList<AggregationRoomLoadInput>> BuildAggregationRooms(
        IEnumerable<Room> sourceRooms,
        Building building,
        CalculationPreferences preferences,
        PipelineClimateContext climateContext,
        string? requestedMethod)
    {
        var roomToZone = building.ThermalZones
            .SelectMany(zone => zone.AssignedRooms.Select(room => new { room.Id, ZoneId = (int?)zone.Id }))
            .GroupBy(item => item.Id)
            .ToDictionary(group => group.Key, group => group.First().ZoneId);
        var rooms = new List<AggregationRoomLoadInput>();

        foreach (var room in sourceRooms.OrderBy(room => room.Id))
        {
            var load = CalculateRoomLoad(
                room,
                preferences,
                climateContext,
                requestedMethod);
            if (load.IsFailure)
                return Result<IReadOnlyList<AggregationRoomLoadInput>>.Failure(load);

            if (load.Value.HasErrors)
                return Result<IReadOnlyList<AggregationRoomLoadInput>>.Validation(FormatErrorDiagnostics(load.Value.Diagnostics));

            rooms.Add(new AggregationRoomLoadInput(
                RoomId: room.Id,
                RoomName: room.Name,
                ThermalZoneId: roomToZone.GetValueOrDefault(room.Id),
                FloorId: room.FloorId,
                BuildingId: building.Id,
                AreaM2: room.Area.SquareMeters,
                HeatingLoadW: load.Value.HeatingLoadW,
                CoolingLoadW: load.Value.CoolingLoadW,
                HeatingBreakdown: load.Value.HeatingBreakdown,
                CoolingBreakdown: load.Value.CoolingBreakdown,
                HourlyHeatingLoadW: Enumerable.Repeat(load.Value.HeatingLoadW, 24).ToArray(),
                HourlyCoolingLoadW: Enumerable.Repeat(load.Value.CoolingLoadW, 24).ToArray()));
        }

        return Result<IReadOnlyList<AggregationRoomLoadInput>>.Success(rooms);
    }

    private AnnualEnergyAdapterInput BuildAnnualEnergyInput(
        Building building,
        BuildingEnergyBalanceResult source)
    {
        if (source.HourlyBalanceRecords.Count > 0)
        {
            var mapping = new HourlySimulationToAnnualEnergyInputMapper().Map(
                building.Id,
                building.Name,
                CalculateBuildingArea(building),
                _timeProvider.GetUtcNow().Year,
                source.HourlyBalanceRecords,
                $"Building {building.Id} application energy balance");

            return new AnnualEnergyAdapterInput(
                mapping.Input,
                TrueHourlySimulationSource,
                mapping.IsTrueHourly8760,
                mapping.HourlyRecordCount,
                mapping.Diagnostics);
        }

        var balances = source.MonthlyBalances.OrderBy(balance => balance.Month).ToArray();
        var hours = new List<AnnualEnergyBalanceHourInput>(balances.Length);
        var diagnosticsForAdapter = new List<CalculationDiagnostic>
        {
            new(
                CalculationDiagnosticSeverity.Info,
                "AnnualEnergy.InternalGainScheduleUnavailableInMonthlyAdapter",
                "Monthly balance adapter does not expose hourly internal gains schedules; annual internal gains are not expanded from room schedules in this path.",
                $"Building {building.Id} application energy balance")
        };

        foreach (var balance in balances)
        {
            var duration = HoursInMonth(balance.Month);
            hours.Add(new AnnualEnergyBalanceHourInput(
                HourIndex: MonthStartHour(balance.Month),
                Month: balance.Month,
                HeatingLoadW: duration > 0 ? balance.HeatingDemandKWh * 1000.0 / duration : 0,
                CoolingLoadW: duration > 0 ? balance.CoolingDemandKWh * 1000.0 / duration : 0,
                HourDurationH: duration));
        }

        return new AnnualEnergyAdapterInput(
            new AnnualEnergyBalanceInput(
                BuildingId: building.Id,
                BuildingName: building.Name,
                BuildingAreaM2: CalculateBuildingArea(building),
                Year: _timeProvider.GetUtcNow().Year,
                Hours: hours,
                UsesSyntheticWeather: true,
                WeatherSource: MonthlyBalanceAdapterSource,
                DiagnosticsContext: $"Building {building.Id} application energy balance",
                EnergyDataSource: MonthlyBalanceAdapterSource,
                IsTrueHourly8760: false,
                ActualMethod: EnergyCalculationParityAnnualAggregationAdapter),
            MonthlyBalanceAdapterSource,
            IsTrueHourly8760: false,
            HourlyRecordCount: hours.Count,
            diagnosticsForAdapter);
    }

    private static RoomCalculationResult MapCoolingRoomResult(
        Room room,
        RoomLoadCalculationResult load,
        CalculationPreferences preferences,
        CoolingLoadCalculationMethod requestedMethod)
    {
        var reserveFactor = preferences.CoolingSafetyFactor;
        var designCapacity = load.CoolingLoadW * reserveFactor;
        var internalGain = load.CoolingBreakdown.InternalGainsW;
        var outdoorTemperature = room.OutdoorTemperatureOverride?.Celsius ??
            room.Floor.Building.ClimateZone?.SummerDesignTemperature.Celsius ??
            room.IndoorTemperature.Celsius;
        var ventilationAssumption = ResolveEffectiveVentilationAssumption(
            room,
            preferences,
            Math.Max(outdoorTemperature - room.IndoorTemperature.Celsius, 0));

        return new RoomCalculationResult
        {
            RoomId = room.Id,
            RoomName = room.Name,
            CalculationMethod = RoomPipelineMethod,
            RequestedMethod = requestedMethod.ToString(),
            ActualMethod = EnergyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation Parity design-point pipeline",
            PeakHour = 15,
            AreaM2 = Round(room.Area.SquareMeters),
            HeightM = Round(room.HeightM),
            VolumeM3 = Round(room.CalculateVolume()),
            IndoorTemperatureC = Round(room.IndoorTemperature.Celsius),
            OutdoorTemperatureC = Round(outdoorTemperature),
            PeopleCount = room.PeopleCount,
            EquipmentLoadW = Round(room.EquipmentLoad.Watts),
            LightingLoadW = Round(room.LightingLoad.Watts),
            EffectiveAirChangesPerHour = Round(ventilationAssumption.EffectiveAirChangesPerHour),
            EffectiveMechanicalAirflowM3PerHour = Round(ventilationAssumption.EffectiveMechanicalAirflowM3PerHour),
            EffectiveInfiltrationAirChangesPerHour = Round(ventilationAssumption.EffectiveInfiltrationAirChangesPerHour),
            EffectiveInfiltrationAirflowM3PerHour = Round(ventilationAssumption.EffectiveInfiltrationAirflowM3PerHour),
            VentilationAssumptionSource = ventilationAssumption.Source,
            TotalWindowAreaM2 = Round(room.Windows.Sum(window => window.Area.SquareMeters)),
            TotalWallAreaM2 = Round(room.Walls.Sum(wall => wall.Area.SquareMeters)),
            ExternalWallAreaM2 = Round(room.Walls.Where(wall => wall.IsExternal).Sum(wall => wall.Area.SquareMeters)),
            WindowHeatGainW = Round(load.CoolingBreakdown.WindowTransmissionW + load.CoolingBreakdown.SolarW),
            WallHeatGainW = Round(load.CoolingBreakdown.TransmissionW + load.CoolingBreakdown.GroundW),
            VentilationHeatGainW = Round(load.CoolingBreakdown.VentilationW),
            InfiltrationHeatGainW = Round(load.CoolingBreakdown.InfiltrationW),
            PeopleHeatGainW = 0,
            EquipmentHeatGainW = Round(room.EquipmentLoad.Watts),
            LightingHeatGainW = Round(room.LightingLoad.Watts),
            InternalHeatGainW = Round(internalGain),
            TotalHeatLoadW = Round(load.CoolingLoadW),
            TotalHeatLoadKw = Round(load.CoolingLoadW / 1000.0),
            CoolingLoadW = Round(load.CoolingLoadW),
            CoolingLoadWPerM2 = Round(load.CoolingLoadWPerM2),
            DeltaTemperatureC = Round(Math.Abs(outdoorTemperature - room.IndoorTemperature.Celsius)),
            HeightAdjustmentFactor = Round(room.HeightM / 3.0),
            TemperatureAdjustmentFactor = 1.0,
            DesignReserveFactor = Round(reserveFactor),
            DesignCapacityW = Round(designCapacity),
            DesignCapacityKw = Round(designCapacity / 1000.0),
            HourlyHeatLoadW = Enumerable.Repeat(Round(load.CoolingLoadW), 24).ToList(),
            Breakdown = load.CoolingBreakdown,
            Diagnostics = load.Diagnostics.ToList(),
            Assumptions = load.AssumptionsUsed.ToList()
        };
    }

    private static RoomHeatingLoadResult MapHeatingRoomResult(
        Room room,
        RoomLoadCalculationResult load,
        HeatingLoadCalculationMethod requestedMethod,
        CalculationPreferences preferences)
    {
        var ventilation = load.HeatingBreakdown.VentilationW;
        var infiltration = load.HeatingBreakdown.InfiltrationW;
        var transmission = load.HeatingBreakdown.TransmissionW +
            load.HeatingBreakdown.WindowTransmissionW +
            load.HeatingBreakdown.GroundW;
        var outdoorDesignTemperature = room.Floor.Building.ClimateZone?.WinterDesignTemperature.Celsius ??
            room.OutdoorTemperatureOverride?.Celsius ??
            0;
        var ventilationAssumption = ResolveEffectiveVentilationAssumption(
            room,
            preferences,
            Math.Max(room.IndoorTemperature.Celsius - outdoorDesignTemperature, 0));

        return new RoomHeatingLoadResult
        {
            RoomId = room.Id,
            RoomName = room.Name,
            CalculationMethod = RoomPipelineMethod,
            RequestedMethod = requestedMethod.ToString(),
            ActualMethod = EnergyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation Parity design-point pipeline",
            IndoorDesignTemperatureC = Round(room.IndoorTemperature.Celsius),
            OutdoorDesignTemperatureC = Round(outdoorDesignTemperature),
            DeltaTemperatureC = Round(Math.Max(room.IndoorTemperature.Celsius - outdoorDesignTemperature, 0)),
            VolumeM3 = Round(room.CalculateVolume()),
            AirChangesPerHour = Round(ventilationAssumption.EffectiveAirChangesPerHour),
            EffectiveAirChangesPerHour = Round(ventilationAssumption.EffectiveAirChangesPerHour),
            EffectiveMechanicalAirflowM3PerHour = Round(ventilationAssumption.EffectiveMechanicalAirflowM3PerHour),
            EffectiveInfiltrationAirChangesPerHour = Round(ventilationAssumption.EffectiveInfiltrationAirChangesPerHour),
            EffectiveInfiltrationAirflowM3PerHour = Round(ventilationAssumption.EffectiveInfiltrationAirflowM3PerHour),
            VentilationAssumptionSource = ventilationAssumption.Source,
            TransmissionHeatLossW = Round(transmission),
            VentilationHeatLossW = Round(ventilation + infiltration),
            MechanicalVentilationHeatLossW = Round(ventilation),
            InfiltrationHeatLossW = Round(infiltration),
            NaturalVentilationHeatLossW = 0,
            TotalDesignHeatingLoadW = Round(load.HeatingLoadW),
            TotalDesignHeatingLoadKw = Round(load.HeatingLoadW / 1000.0),
            HeatingLoadW = Round(load.HeatingLoadW),
            HeatingLoadWPerM2 = Round(load.HeatingLoadWPerM2),
            Breakdown = load.HeatingBreakdown,
            Diagnostics = load.Diagnostics.ToList(),
            Assumptions = load.AssumptionsUsed.ToList()
        };
    }

    private static FloorCalculationResult MapFloorResult(
        Floor floor,
        LoadAggregationResult aggregation,
        CalculationPreferences preferences,
        string? requestedMethod)
    {
        var designCapacity = aggregation.CoolingLoadW * preferences.CoolingSafetyFactor;
        var diagnostics = aggregation.Diagnostics.ToList();
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            requestedMethod,
            $"Floor {floor.Id} application aggregation",
            "Energy Calculation Parity design-point aggregation");

        return new FloorCalculationResult
        {
            FloorId = floor.Id,
            FloorName = floor.Name,
            CalculationMethod = AggregationPipelineMethod,
            RequestedMethod = requestedMethod ?? string.Empty,
            ActualMethod = EnergyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation Parity design-point aggregation",
            PeakHour = null,
            RoomsCount = aggregation.RoomCount,
            TotalHeatLoadW = Round(aggregation.CoolingLoadW),
            TotalHeatLoadKw = Round(aggregation.CoolingLoadW / 1000.0),
            CoolingLoadW = Round(aggregation.CoolingLoadW),
            CoolingLoadWPerM2 = Round(aggregation.CoolingLoadWPerM2),
            HeatingLoadW = Round(aggregation.HeatingLoadW),
            HeatingLoadWPerM2 = Round(aggregation.HeatingLoadWPerM2),
            DesignReserveFactor = Round(preferences.CoolingSafetyFactor),
            DesignCapacityW = Round(designCapacity),
            DesignCapacityKw = Round(designCapacity / 1000.0),
            HourlyHeatLoadW = Enumerable.Repeat(Round(aggregation.CoolingLoadW), 24).ToList(),
            ComponentBreakdown = aggregation.ComponentBreakdown,
            Diagnostics = diagnostics
        };
    }

    private static BuildingCalculationResult MapBuildingCoolingResult(
        Building building,
        LoadAggregationResult aggregation,
        CalculationPreferences preferences,
        CoolingLoadCalculationMethod requestedMethod)
    {
        var designCapacity = aggregation.CoolingLoadW * preferences.CoolingSafetyFactor;
        var diagnostics = aggregation.Diagnostics.ToList();
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            requestedMethod.ToString(),
            $"Building {building.Id} application cooling aggregation",
            "Energy Calculation Parity design-point aggregation");

        return new BuildingCalculationResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            CalculationMethod = AggregationPipelineMethod,
            RequestedMethod = requestedMethod.ToString(),
            ActualMethod = EnergyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation Parity design-point aggregation",
            PeakHour = null,
            FloorsCount = building.Floors.Count,
            RoomsCount = aggregation.RoomCount,
            TotalHeatLoadW = Round(aggregation.CoolingLoadW),
            TotalHeatLoadKw = Round(aggregation.CoolingLoadW / 1000.0),
            CoolingLoadW = Round(aggregation.CoolingLoadW),
            CoolingLoadWPerM2 = Round(aggregation.CoolingLoadWPerM2),
            DesignReserveFactor = Round(preferences.CoolingSafetyFactor),
            DesignCapacityW = Round(designCapacity),
            DesignCapacityKw = Round(designCapacity / 1000.0),
            HourlyHeatLoadW = Enumerable.Repeat(Round(aggregation.CoolingLoadW), 24).ToList(),
            ThermalZones = BuildThermalZoneResults(building, aggregation),
            ComponentBreakdown = aggregation.ComponentBreakdown,
            Diagnostics = diagnostics
        };
    }

    private static BuildingHeatingLoadResult MapBuildingHeatingResult(
        Building building,
        LoadAggregationResult aggregation,
        IReadOnlyList<RoomHeatingLoadResult> rooms,
        HeatingLoadCalculationMethod requestedMethod)
    {
        var transmission = rooms.Sum(room => room.TransmissionHeatLossW);
        var ventilation = rooms.Sum(room => room.VentilationHeatLossW);
        var diagnostics = aggregation.Diagnostics.ToList();
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            requestedMethod.ToString(),
            $"Building {building.Id} application heating aggregation",
            "Energy Calculation Parity design-point aggregation");

        return new BuildingHeatingLoadResult
        {
            BuildingId = building.Id,
            ProjectName = building.Project?.Name ?? string.Empty,
            BuildingName = building.Name,
            CalculationMethod = AggregationPipelineMethod,
            RequestedMethod = requestedMethod.ToString(),
            ActualMethod = EnergyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation Parity design-point aggregation",
            RoomsCount = aggregation.RoomCount,
            TransmissionHeatLossW = Round(transmission),
            VentilationHeatLossW = Round(ventilation),
            TotalDesignHeatingLoadW = Round(aggregation.HeatingLoadW),
            TotalDesignHeatingLoadKw = Round(aggregation.HeatingLoadW / 1000.0),
            HeatingLoadW = Round(aggregation.HeatingLoadW),
            HeatingLoadWPerM2 = Round(aggregation.HeatingLoadWPerM2),
            Rooms = rooms.ToList(),
            ComponentBreakdown = aggregation.ComponentBreakdown,
            Diagnostics = diagnostics
        };
    }

    private static BuildingEnergyBalanceResult MapEnergyBalanceResult(
        BuildingEnergyBalanceResult source,
        AnnualEnergyBalanceResult annual,
        CoolingLoadCalculationMethod coolingMethod,
        HeatingLoadCalculationMethod heatingMethod,
        string sourceName,
        bool isTrueHourly8760,
        int hourlyRecordCount,
        IReadOnlyList<CalculationDiagnostic> adapterDiagnostics)
    {
        var diagnostics = source.Diagnostics
            .Concat(annual.Diagnostics)
            .Concat(adapterDiagnostics)
            .ToList();
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            coolingMethod.ToString(),
            $"Building {annual.BuildingId} application annual energy balance cooling method",
            "Energy Calculation Parity annual aggregation adapter");
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            heatingMethod.ToString(),
            $"Building {annual.BuildingId} application annual energy balance heating method",
            "Energy Calculation Parity annual aggregation adapter");

        return new BuildingEnergyBalanceResult
        {
            BuildingId = annual.BuildingId,
            BuildingName = annual.BuildingName ?? source.BuildingName,
            CoolingCalculationMethod = "Energy Calculation Parity / Annual Aggregation Adapter",
            HeatingCalculationMethod = "Energy Calculation Parity / Annual Aggregation Adapter",
            RequestedCoolingMethod = coolingMethod.ToString(),
            RequestedHeatingMethod = heatingMethod.ToString(),
            ActualMethod = EnergyCalculationParityAnnualAggregationAdapter,
            CalculationMethodLabel = "Energy Calculation Parity annual aggregation adapter",
            EnergyDataSource = sourceName,
            IsTrueHourly8760 = isTrueHourly8760,
            HourlyRecordCount = hourlyRecordCount,
            AnnualCoolingDemandKWh = Round(annual.AnnualCoolingDemandKWh),
            AnnualHeatingDemandKWh = Round(annual.AnnualHeatingDemandKWh),
            AnnualTotalDemandKWh = Round(annual.AnnualTotalDemandKWh),
            EnergyUseIntensityKWhPerM2Year = Round(annual.EnergyUseIntensityKWhPerM2Year),
            PeakHeatingW = Round(annual.PeakHeatingLoadW),
            PeakCoolingW = Round(annual.PeakCoolingLoadW),
            PeakHeatingHour = annual.PeakHeatingHour,
            PeakCoolingHour = annual.PeakCoolingHour,
            ComponentBreakdown = annual.ComponentBreakdown,
            MonthlyBalances = annual.MonthlyResults
                .Select(month => new MonthlyEnergyBalance
                {
                    Month = month.Month,
                    HeatingDemandKWh = Round(month.HeatingKWh),
                    CoolingDemandKWh = Round(month.CoolingKWh)
                })
                .ToList(),
            Diagnostics = diagnostics,
            Assumptions = annual.AssumptionsUsed.ToList()
        };
    }

    private static List<ThermalZoneCalculationResult> BuildThermalZoneResults(
        Building building,
        LoadAggregationResult aggregation)
    {
        if (building.ThermalZones.Count == 0)
            return [];

        var roomLoads = aggregation.RoomBreakdown.ToDictionary(room => room.RoomId);
        var countedRooms = new HashSet<int>();
        var results = new List<ThermalZoneCalculationResult>();

        foreach (var zone in building.ThermalZones.OrderBy(zone => zone.Id))
        {
            var rooms = zone.AssignedRooms
                .Where(room => countedRooms.Add(room.Id) && roomLoads.ContainsKey(room.Id))
                .ToArray();
            if (rooms.Length == 0)
                continue;

            var coolingLoad = rooms.Sum(room => roomLoads[room.Id].CoolingLoadW);
            results.Add(new ThermalZoneCalculationResult
            {
                ThermalZoneId = zone.Id,
                ThermalZoneName = zone.Name,
                RoomsCount = rooms.Length,
                TotalHeatLoadW = Round(coolingLoad),
                TotalHeatLoadKw = Round(coolingLoad / 1000.0),
                RoomIds = rooms.Select(room => room.Id).ToList(),
                HourlyHeatLoadW = Enumerable.Repeat(Round(coolingLoad), 24).ToList()
            });
        }

        var unassigned = building.Floors
            .SelectMany(floor => floor.Rooms)
            .Where(room => !countedRooms.Contains(room.Id) && roomLoads.ContainsKey(room.Id))
            .ToArray();
        if (unassigned.Length > 0)
        {
            var coolingLoad = unassigned.Sum(room => roomLoads[room.Id].CoolingLoadW);
            results.Add(new ThermalZoneCalculationResult
            {
                ThermalZoneName = "Unassigned rooms",
                IsUnassignedRoomsZone = true,
                RoomsCount = unassigned.Length,
                TotalHeatLoadW = Round(coolingLoad),
                TotalHeatLoadKw = Round(coolingLoad / 1000.0),
                RoomIds = unassigned.Select(room => room.Id).ToList(),
                HourlyHeatLoadW = Enumerable.Repeat(Round(coolingLoad), 24).ToList()
            });
        }

        return results;
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

    private static double CalculateBuildingArea(Building building) =>
        building.Floors.SelectMany(floor => floor.Rooms).Sum(room => room.Area.SquareMeters);

    private static int HoursInMonth(int month)
    {
        var days = month switch
        {
            1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
            4 or 6 or 9 or 11 => 30,
            2 => 28,
            _ => 30
        };

        return days * 24;
    }

    private static int MonthStartHour(int month)
    {
        var hour = 0;
        for (var current = 1; current < month; current++)
            hour += HoursInMonth(current);

        return hour;
    }

    private static bool HasCompleteAnnualClimateData(AnnualClimateData? annualData) =>
        annualData?.HourlyData
            .Select(hour => hour.HourOfYear)
            .Distinct()
            .Count() == 8760;

    private sealed record PipelineClimateContext(
        AnnualClimateData? AnnualClimateData,
        bool IsCompleteAnnualClimateData);

    private sealed record RoomGroundContext(
        double? HeatingGroundTemperatureC,
        double? CoolingGroundTemperatureC,
        IReadOnlyList<CalculationDiagnostic> Diagnostics,
        IReadOnlyList<string> Assumptions)
    {
        public static RoomGroundContext Empty { get; } = new(null, null, [], []);
    }

    private sealed record RoomSolarContext(
        IReadOnlyDictionary<int, double> IrradianceByWindowId,
        IReadOnlyList<CalculationDiagnostic> Diagnostics,
        IReadOnlyList<string> Assumptions)
    {
        public static RoomSolarContext Empty { get; } = new(
            new Dictionary<int, double>(),
            [],
            []);
    }

    private sealed record AnnualEnergyAdapterInput(
        AnnualEnergyBalanceInput Input,
        string Source,
        bool IsTrueHourly8760,
        int HourlyRecordCount,
        IReadOnlyList<CalculationDiagnostic> Diagnostics);

    private sealed record EffectiveVentilationAssumption(
        double EffectiveAirChangesPerHour,
        double EffectiveMechanicalAirflowM3PerHour,
        double EffectiveInfiltrationAirChangesPerHour,
        double EffectiveInfiltrationAirflowM3PerHour,
        string Source);

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
