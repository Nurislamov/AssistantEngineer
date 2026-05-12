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

public sealed partial class EnergyCalculationPipelineService
{

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
        var result = _roomLoadEngine.Calculate(input);
        if (result.IsFailure)
            return result;

        _logger.LogDebug(
            "Standard-Based Calculation room load calculated for room {RoomId}: heating {HeatingLoadW} W, cooling {CoolingLoadW} W.",
            room.Id,
            result.Value.HeatingLoadW,
            result.Value.CoolingLoadW);

        return result;
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