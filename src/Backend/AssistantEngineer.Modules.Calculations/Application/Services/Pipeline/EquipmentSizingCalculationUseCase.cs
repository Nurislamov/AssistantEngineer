using System.Globalization;
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
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

public sealed class EquipmentSizingCalculationUseCase : IEquipmentSizingCalculationUseCase
{
    private readonly IRoomRepository _rooms;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly RoomLoadCalculationEngine _roomLoadEngine;
    private readonly ICoolingLoadReferenceData _coolingReferenceData;
    private readonly ICoolingEquipmentCatalogSizingProvider? _equipmentCatalogSizingProvider;
    private readonly CoolingLoadCalculationOptions _coolingOptions;
    private readonly En12831HeatingLoadOptions _heatingOptions;
    private readonly Iso52016EnergyNeedOptions _energyNeedOptions;
    private readonly EnergyCalculationPipelineClimateContextBuilder _climateContextBuilder;
    private readonly EnergyCalculationPipelineRoomContextResolver _roomContextResolver;
    private readonly EnergyCalculationPipelineEquipmentSizingOrchestrator _equipmentSizingOrchestrator;
    private readonly EnergyCalculationPipelineDiagnosticsPolicy _diagnosticsPolicy;

    public EquipmentSizingCalculationUseCase(
        IRoomRepository rooms,
        ICalculationPreferencesRepository preferences,
        RoomLoadCalculationEngine roomLoadEngine,
        EquipmentSizingEngine equipmentSizingEngine,
        ICoolingLoadReferenceData coolingReferenceData,
        IOptions<CoolingLoadCalculationOptions> coolingOptions,
        IOptions<En12831HeatingLoadOptions> heatingOptions,
        ICoolingEquipmentCatalogSizingProvider? equipmentCatalogSizingProvider = null,
        IAnnualClimateDataProvider? annualClimateDataProvider = null,
        IGroundTemperatureService? groundTemperatureService = null,
        ISolarRadiationService? solarRadiationService = null,
        IOptions<Iso52016EnergyNeedOptions>? energyNeedOptions = null)
    {
        _rooms = rooms;
        _preferences = preferences;
        _roomLoadEngine = roomLoadEngine;
        _coolingReferenceData = coolingReferenceData;
        _coolingOptions = coolingOptions.Value;
        _heatingOptions = heatingOptions.Value;
        _energyNeedOptions = energyNeedOptions?.Value ?? new Iso52016EnergyNeedOptions();
        _equipmentCatalogSizingProvider = equipmentCatalogSizingProvider;
        _climateContextBuilder = new EnergyCalculationPipelineClimateContextBuilder(
            annualClimateDataProvider,
            _energyNeedOptions,
            NullLogger<EnergyCalculationPipelineService>.Instance);
        _roomContextResolver = new EnergyCalculationPipelineRoomContextResolver(
            _coolingReferenceData,
            groundTemperatureService,
            solarRadiationService,
            _energyNeedOptions);
        _equipmentSizingOrchestrator = new EnergyCalculationPipelineEquipmentSizingOrchestrator(equipmentSizingEngine);
        _diagnosticsPolicy = new EnergyCalculationPipelineDiagnosticsPolicy();
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
        var roomFailure = _diagnosticsPolicy.TryMapRoomLoadFailureOrValidation<EquipmentSizingResult>(load);
        if (roomFailure is not null)
            return roomFailure;

        return await _equipmentSizingOrchestrator.CalculateForRoomAsync(
            room,
            load.Value,
            preferences,
            systemType,
            unitType,
            _equipmentCatalogSizingProvider,
            cancellationToken);
    }

    private async Task<CalculationPreferences> GetPreferencesAsync(
        int projectId,
        CancellationToken cancellationToken) =>
        await _preferences.GetByProjectIdAsync(projectId, cancellationToken) ??
        CalculationPreferences.Default();

    private Result<RoomLoadCalculationResult> CalculateRoomLoad(
        Room room,
        CalculationPreferences preferences,
        PipelineClimateContext climateContext,
        string? requestedMethod = null)
    {
        if (room.Floor.Building.ClimateZone is null)
            return Result<RoomLoadCalculationResult>.Validation("Building climate zone is required for Standard-Based Calculation room load calculation.");

        var input = BuildRoomLoadInput(
            room,
            preferences,
            climateContext,
            requestedMethod);
        return _roomLoadEngine.Calculate(input);
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

    private InternalGainInput CreateInternalGainInput(Room room) =>
        new(
            RoomId: room.Id,
            AreaM2: room.Area.SquareMeters,
            OccupancyPeople: room.PeopleCount,
            SensibleGainPerPersonW: _coolingReferenceData.GetPeopleHeatGainW(room.Type),
            EquipmentLoadW: room.EquipmentLoad.Watts,
            LightingLoadW: room.LightingLoad.Watts,
            DiagnosticsContext: $"Room {room.Id} application internal gains");

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

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
            "Standard-Based Calculation design-point calculation pipeline");
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
}
