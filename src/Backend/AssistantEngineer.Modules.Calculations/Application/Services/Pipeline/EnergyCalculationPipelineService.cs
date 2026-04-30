using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
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
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

public sealed class EnergyCalculationPipelineService
{
    private const string RoomPipelineMethod = "Energy Calculation Parity / Application Room Load Pipeline";
    private const string AggregationPipelineMethod = "Energy Calculation Parity / Application Load Aggregation Pipeline";

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
    private readonly CoolingLoadCalculationOptions _coolingOptions;
    private readonly En12831HeatingLoadOptions _heatingOptions;
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
        _timeProvider = timeProvider;
        _equipmentCatalogSizingProvider = equipmentCatalogSizingProvider;
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
        return CalculateRoomLoad(room, preferences);
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
        var load = CalculateRoomLoad(room, preferences);
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
        var load = CalculateRoomLoad(room, preferences);
        if (load.IsFailure)
            return Result<RoomHeatingLoadResult>.Failure(load);

        if (load.Value.HasErrors)
            return Result<RoomHeatingLoadResult>.Validation(FormatErrorDiagnostics(load.Value.Diagnostics));

        return Result<RoomHeatingLoadResult>.Success(MapHeatingRoomResult(room, load.Value, method));
    }

    public async Task<Result<FloorCalculationResult>> CalculateFloorLoadAsync(
        int floorId,
        CancellationToken cancellationToken = default)
    {
        var floor = await _floors.GetForCalculationAsync(floorId, cancellationToken);
        if (floor is null)
            return Result<FloorCalculationResult>.NotFound($"Floor with id {floorId} not found.");

        var preferences = await GetPreferencesAsync(floor.Building.ProjectId, cancellationToken);
        var aggregation = AggregateFloor(floor, preferences);
        if (aggregation.IsFailure)
            return Result<FloorCalculationResult>.Failure(aggregation);

        if (aggregation.Value.HasErrors)
            return Result<FloorCalculationResult>.Validation(FormatErrorDiagnostics(aggregation.Value.Diagnostics));

        return Result<FloorCalculationResult>.Success(MapFloorResult(floor, aggregation.Value, preferences));
    }

    public Task<Result<FloorCalculationResult>> CalculateFloorCoolingLoadAsync(
        int floorId,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default) =>
        CalculateFloorLoadAsync(floorId, cancellationToken);

    public Task<Result<FloorCalculationResult>> CalculateFloorHeatingLoadAsync(
        int floorId,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CancellationToken cancellationToken = default) =>
        CalculateFloorLoadAsync(floorId, cancellationToken);

    public async Task<Result<BuildingCalculationResult>> CalculateBuildingCoolingLoadAsync(
        int buildingId,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingCalculationResult>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await GetPreferencesAsync(building.ProjectId, cancellationToken);
        var aggregation = AggregateBuilding(building, preferences);
        if (aggregation.IsFailure)
            return Result<BuildingCalculationResult>.Failure(aggregation);

        if (aggregation.Value.HasErrors)
            return Result<BuildingCalculationResult>.Validation(FormatErrorDiagnostics(aggregation.Value.Diagnostics));

        return Result<BuildingCalculationResult>.Success(MapBuildingCoolingResult(building, aggregation.Value, preferences));
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
        var aggregation = AggregateBuilding(building, preferences);
        if (aggregation.IsFailure)
            return Result<BuildingHeatingLoadResult>.Failure(aggregation);

        if (aggregation.Value.HasErrors)
            return Result<BuildingHeatingLoadResult>.Validation(FormatErrorDiagnostics(aggregation.Value.Diagnostics));

        var roomResults = new List<RoomHeatingLoadResult>();
        foreach (var room in building.Floors.SelectMany(floor => floor.Rooms).OrderBy(room => room.Id))
        {
            var roomLoad = CalculateRoomLoad(room, preferences);
            if (roomLoad.IsFailure)
                return Result<BuildingHeatingLoadResult>.Failure(roomLoad);

            if (roomLoad.Value.HasErrors)
                return Result<BuildingHeatingLoadResult>.Validation(FormatErrorDiagnostics(roomLoad.Value.Diagnostics));

            roomResults.Add(MapHeatingRoomResult(room, roomLoad.Value, method));
        }

        return Result<BuildingHeatingLoadResult>.Success(MapBuildingHeatingResult(
            building,
            aggregation.Value,
            roomResults));
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
        var annualInput = BuildAnnualEnergyInput(building, source);
        var annual = _annualEnergyEngine.Calculate(annualInput);
        if (annual.IsFailure)
            return Result<BuildingEnergyBalanceResult>.Failure(annual);

        var result = MapEnergyBalanceResult(source, annual.Value, coolingMethod, heatingMethod);
        if (source.MonthlyBalances.Count == 0)
        {
            result.Diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "AnnualEnergy.HourlySourceUnavailable",
                "Existing hourly/monthly source data was unavailable; annual energy balance returned zero-load diagnostic output.",
                $"Building {building.Id}"));
        }

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
        var load = CalculateRoomLoad(room, preferences);
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
                HeatingCapacityW: null,
                CoolingCapacityW: candidate.NominalCoolingCapacityKw * 1000,
                IsActive: true))
            .ToArray();

        var sizing = _equipmentSizingEngine.Calculate(new EquipmentSizingInput(
            TargetId: room.Id,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 0,
            RequiredCoolingLoadW: load.Value.CoolingLoadW,
            SafetyFactor: preferences.CoolingSafetyFactor,
            Candidates: candidates,
            DiagnosticsContext: $"Room {room.Id} equipment selection"));

        if (sizing.IsFailure)
            return sizing;

        var diagnostics = sizing.Value.Diagnostics.ToList();
        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "EquipmentSizing.CoolingCatalogOnly",
            "Cooling equipment catalog does not expose heating capacity; sizing evaluated cooling capacity only.",
            $"Room {room.Id} equipment selection"));

        return Result<EquipmentSizingResult>.Success(sizing.Value with { Diagnostics = diagnostics });
    }

    private Result<RoomLoadCalculationResult> CalculateRoomLoad(
        Room room,
        CalculationPreferences preferences)
    {
        if (room.Floor.Building.ClimateZone is null)
            return Result<RoomLoadCalculationResult>.Validation("Building climate zone is required for Energy Calculation Parity room load calculation.");

        var input = BuildRoomLoadInput(room, preferences);
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
        CalculationPreferences preferences)
    {
        var indoor = room.IndoorTemperature.Celsius;
        var heatingOutdoor = room.Floor.Building.ClimateZone?.WinterDesignTemperature.Celsius ??
            room.OutdoorTemperatureOverride?.Celsius ??
            _heatingOptions.DefaultOutdoorHeatingDesignTemperatureC;
        var coolingOutdoor = room.OutdoorTemperatureOverride?.Celsius ??
            room.Floor.Building.ClimateZone?.SummerDesignTemperature.Celsius ??
            _coolingOptions.DefaultOutdoorCoolingDesignTemperatureC;

        var heatingTransmission = RoomTransmissionInputFactory.CreateForRoom(
            room,
            indoor,
            heatingOutdoor).Elements;
        var coolingTransmission = RoomTransmissionInputFactory.CreateForRoom(
            room,
            indoor,
            coolingOutdoor).Elements;

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
            WindowSolarGains: CreateSolarInput(room),
            HeatingVentilationAndInfiltration: CreateVentilationInput(
                room,
                preferences,
                indoor,
                heatingOutdoor,
                isHeating: true),
            CoolingVentilationAndInfiltration: CreateVentilationInput(
                room,
                preferences,
                indoor,
                coolingOutdoor,
                isHeating: false),
            InternalGains: CreateInternalGainInput(room),
            DiagnosticsContext: $"Room {room.Id} application load pipeline");
    }

    private RoomWindowSolarGainRequest? CreateSolarInput(Room room)
    {
        if (room.Windows.Count == 0)
            return null;

        var windows = room.Windows
            .Select(window => WindowSolarGainInputFactory.CreateForWindow(
                window,
                _coolingReferenceData.GetWindowSolarLoadWPerM2(window.Orientation),
                diagnosticsContext: $"Room {room.Id} window {window.Id} application solar gain"))
            .ToArray();

        return new RoomWindowSolarGainRequest(room.Id, windows);
    }

    private VentilationAndInfiltrationLoadInput CreateVentilationInput(
        Room room,
        CalculationPreferences preferences,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        bool isHeating)
    {
        var deltaT = isHeating
            ? Math.Max(indoorTemperatureC - outdoorTemperatureC, 0)
            : Math.Max(outdoorTemperatureC - indoorTemperatureC, 0);
        var ventilation = room.VentilationParameters;
        var infiltrationAirChangesPerHour = ventilation is null
            ? 0
            : ventilation.InfiltrationAirChangesPerHour + ventilation.StackCoefficient * Math.Sqrt(deltaT);

        return new VentilationAndInfiltrationLoadInput(
            RoomId: room.Id,
            AreaM2: room.Area.SquareMeters,
            VolumeM3: room.CalculateVolume(),
            OccupancyPeople: room.PeopleCount,
            IndoorTemperatureC: indoorTemperatureC,
            OutdoorTemperatureC: outdoorTemperatureC,
            AirChangesPerHour: ventilation?.AirChangesPerHour ??
                preferences.Iso52016DefaultAirChangesPerHour,
            InfiltrationAirChangesPerHour: infiltrationAirChangesPerHour,
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
        CalculationPreferences preferences)
    {
        var rooms = BuildAggregationRooms(floor.Rooms, floor.Building, preferences);
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
        CalculationPreferences preferences)
    {
        var rooms = BuildAggregationRooms(
            building.Floors.SelectMany(floor => floor.Rooms),
            building,
            preferences);
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
        CalculationPreferences preferences)
    {
        var roomToZone = building.ThermalZones
            .SelectMany(zone => zone.AssignedRooms.Select(room => new { room.Id, ZoneId = (int?)zone.Id }))
            .GroupBy(item => item.Id)
            .ToDictionary(group => group.Key, group => group.First().ZoneId);
        var rooms = new List<AggregationRoomLoadInput>();

        foreach (var room in sourceRooms.OrderBy(room => room.Id))
        {
            var load = CalculateRoomLoad(room, preferences);
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

    private AnnualEnergyBalanceInput BuildAnnualEnergyInput(
        Building building,
        BuildingEnergyBalanceResult source)
    {
        var balances = source.MonthlyBalances.Count == 0
            ? Enumerable.Range(1, 12).Select(month => new MonthlyEnergyBalance { Month = month }).ToArray()
            : source.MonthlyBalances.OrderBy(balance => balance.Month).ToArray();
        var hours = new List<AnnualEnergyBalanceHourInput>(balances.Length);

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

        return new AnnualEnergyBalanceInput(
            BuildingId: building.Id,
            BuildingName: building.Name,
            BuildingAreaM2: CalculateBuildingArea(building),
            Year: _timeProvider.GetUtcNow().Year,
            Hours: hours,
            UsesSyntheticWeather: true,
            WeatherSource: source.MonthlyBalances.Count == 0 ? "unavailable" : "synthetic profile",
            DiagnosticsContext: $"Building {building.Id} application energy balance");
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

        return new RoomCalculationResult
        {
            RoomId = room.Id,
            RoomName = room.Name,
            CalculationMethod = $"{requestedMethod} via {RoomPipelineMethod}",
            PeakHour = 15,
            AreaM2 = Round(room.Area.SquareMeters),
            HeightM = Round(room.HeightM),
            VolumeM3 = Round(room.CalculateVolume()),
            IndoorTemperatureC = Round(room.IndoorTemperature.Celsius),
            OutdoorTemperatureC = Round(outdoorTemperature),
            PeopleCount = room.PeopleCount,
            EquipmentLoadW = Round(room.EquipmentLoad.Watts),
            LightingLoadW = Round(room.LightingLoad.Watts),
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
        HeatingLoadCalculationMethod requestedMethod)
    {
        var ventilation = load.HeatingBreakdown.VentilationW;
        var infiltration = load.HeatingBreakdown.InfiltrationW;
        var transmission = load.HeatingBreakdown.TransmissionW +
            load.HeatingBreakdown.WindowTransmissionW +
            load.HeatingBreakdown.GroundW;

        return new RoomHeatingLoadResult
        {
            RoomId = room.Id,
            RoomName = room.Name,
            CalculationMethod = $"{requestedMethod} via {RoomPipelineMethod}",
            IndoorDesignTemperatureC = Round(room.IndoorTemperature.Celsius),
            OutdoorDesignTemperatureC = Round(room.Floor.Building.ClimateZone?.WinterDesignTemperature.Celsius ?? 0),
            DeltaTemperatureC = Round(Math.Max(room.IndoorTemperature.Celsius - (room.Floor.Building.ClimateZone?.WinterDesignTemperature.Celsius ?? 0), 0)),
            VolumeM3 = Round(room.CalculateVolume()),
            AirChangesPerHour = Round((room.VentilationParameters?.AirChangesPerHour ?? 0) + (room.VentilationParameters?.InfiltrationAirChangesPerHour ?? 0)),
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
        CalculationPreferences preferences)
    {
        var designCapacity = aggregation.CoolingLoadW * preferences.CoolingSafetyFactor;

        return new FloorCalculationResult
        {
            FloorId = floor.Id,
            FloorName = floor.Name,
            CalculationMethod = AggregationPipelineMethod,
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
            Diagnostics = aggregation.Diagnostics.ToList()
        };
    }

    private static BuildingCalculationResult MapBuildingCoolingResult(
        Building building,
        LoadAggregationResult aggregation,
        CalculationPreferences preferences)
    {
        var designCapacity = aggregation.CoolingLoadW * preferences.CoolingSafetyFactor;

        return new BuildingCalculationResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            CalculationMethod = AggregationPipelineMethod,
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
            Diagnostics = aggregation.Diagnostics.ToList()
        };
    }

    private static BuildingHeatingLoadResult MapBuildingHeatingResult(
        Building building,
        LoadAggregationResult aggregation,
        IReadOnlyList<RoomHeatingLoadResult> rooms)
    {
        var transmission = rooms.Sum(room => room.TransmissionHeatLossW);
        var ventilation = rooms.Sum(room => room.VentilationHeatLossW);

        return new BuildingHeatingLoadResult
        {
            BuildingId = building.Id,
            ProjectName = building.Project?.Name ?? string.Empty,
            BuildingName = building.Name,
            CalculationMethod = AggregationPipelineMethod,
            RoomsCount = aggregation.RoomCount,
            TransmissionHeatLossW = Round(transmission),
            VentilationHeatLossW = Round(ventilation),
            TotalDesignHeatingLoadW = Round(aggregation.HeatingLoadW),
            TotalDesignHeatingLoadKw = Round(aggregation.HeatingLoadW / 1000.0),
            HeatingLoadW = Round(aggregation.HeatingLoadW),
            HeatingLoadWPerM2 = Round(aggregation.HeatingLoadWPerM2),
            Rooms = rooms.ToList(),
            ComponentBreakdown = aggregation.ComponentBreakdown,
            Diagnostics = aggregation.Diagnostics.ToList()
        };
    }

    private static BuildingEnergyBalanceResult MapEnergyBalanceResult(
        BuildingEnergyBalanceResult source,
        AnnualEnergyBalanceResult annual,
        CoolingLoadCalculationMethod coolingMethod,
        HeatingLoadCalculationMethod heatingMethod) =>
        new()
        {
            BuildingId = annual.BuildingId,
            BuildingName = annual.BuildingName ?? source.BuildingName,
            CoolingCalculationMethod = $"{coolingMethod} via {annual.CalculationMethod}",
            HeatingCalculationMethod = $"{heatingMethod} via {annual.CalculationMethod}",
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
            Diagnostics = annual.Diagnostics.ToList(),
            Assumptions = annual.AssumptionsUsed.ToList()
        };

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

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
