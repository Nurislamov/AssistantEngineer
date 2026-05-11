using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class Iso52016Registration
{
    public static IServiceCollection AddIso52016Calculations(
        this IServiceCollection services)
    {
        services.AddScoped<ISo52016ReferenceDataProvider, Iso52016ReferenceDataProvider>();
        services.AddScoped<Iso52016ClimateDataValidator>();

        services.AddScoped<ISolarRadiationService, SolarRadiationService>();
        services.AddScoped<IWindowShadingService, WindowShadingService>();

        services.AddScoped<ISo52016GroundBoundaryTemperatureProvider, PeriodicIso52016GroundBoundaryTemperatureProvider>();
        services.AddScoped<ISo52016WeatherSolarContextBuilder, Iso52016WeatherSolarContextBuilder>();

        services.AddSingleton<Iso52016ConstructionReferenceDataProvider>();
        services.AddSingleton<Iso52016ConstructionAssemblyCalculator>();
        services.AddSingleton<Iso52016ConstructionAssemblyApplicationAdapter>();

        services.AddScoped<ISo52016WindowSolarGainCalculator, Iso52016WindowSolarGainCalculator>();
        services.AddScoped<ISo52016WindowSolarGainProfileBuilder, Iso52016WindowSolarGainProfileBuilder>();
        services.AddScoped<ISo52016RoomSolarGainProfileBuilder, Iso52016RoomSolarGainProfileBuilder>();

        services.AddScoped<InternalGainEngine>();
        services.AddScoped<ISo52016RoomInternalGainProfileBuilder, Iso52016RoomInternalGainProfileBuilder>();

        services.AddScoped<ISo52016RoomHourlyInputProfileBuilder, Iso52016RoomHourlyInputProfileBuilder>();
        services.AddScoped<ISo52016RoomEnergySimulationService, Iso52016RoomEnergySimulationService>();

        services.AddScoped<ISo52016ScheduleProfileExpander, Iso52016ScheduleProfileExpander>();
        services.AddScoped<ISo52016RoomEnvelopeInputCalculator, Iso52016RoomEnvelopeInputCalculator>();
        services.AddScoped<ISo52016RoomWindowSolarGainInputMapper, Iso52016RoomWindowSolarGainInputMapper>();
        services.AddScoped<ISo52016RoomEnergySimulationRequestBuilder, Iso52016RoomEnergySimulationRequestBuilder>();

        services.AddScoped<ISo52016RoomSimulationFacade, Iso52016RoomSimulationFacade>();
        services.AddScoped<ISo52016BuildingSimulationFacade, Iso52016BuildingSimulationFacade>();

        services.AddScoped<ISo52016BuildingRoomCollector, Iso52016BuildingRoomCollector>();
        services.AddScoped<ISo52016BuildingDomainSimulationFacade, Iso52016BuildingDomainSimulationFacade>();
        services.AddScoped<ISo52016MultiZoneInputValidator, Iso52016MultiZoneInputValidator>();
        services.AddScoped<ISo52016MultiZoneGraphBuilder, Iso52016MultiZoneGraphBuilder>();
        services.AddScoped<ISo52016MultiZoneHourlySolver, Iso52016MultiZoneHourlySolver>();
        services.AddScoped<ISo52016MultiZoneEnergySimulationService, Iso52016MultiZoneEnergySimulationService>();
        services.AddScoped<ISo52016MultiZoneBuildingSimulationFacade, Iso52016MultiZoneBuildingSimulationFacade>();
        services.AddScoped<IAdjacentUnconditionedZoneTemperatureCalculator, AdjacentUnconditionedZoneTemperatureCalculator>();
        services.AddScoped<ISo52016MultiZoneNormalizedInputBuilder, Iso52016MultiZoneNormalizedInputBuilder>();

        services.AddScoped<ISo52016BuildingEnergySimulationApplicationService, Iso52016BuildingEnergySimulationApplicationService>();

        services.AddScoped<Iso52016HourlySteadyStateCalculator>();
        services.AddScoped<Iso52016MonthlyQuasiSteadyStateCalculator>();

        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.ISo52016MatrixHourlySolver, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016MatrixHourlySolver>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.ISo52016InternalGainReferenceDataProvider, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016InternalGainReferenceDataProvider>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.ISo52016AdjacentUnconditionedZoneTemperatureSolver, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016AdjacentUnconditionedZoneTemperatureSolver>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.ISo52016MatrixReducedRoomModelBuilder, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016MatrixReducedRoomModelBuilder>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.ISo52016PhysicalRoomModelBuilder, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalRoomModelBuilder>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.ISo52016MatrixRoomEnergySimulationService, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016MatrixRoomEnergySimulationService>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.ISo52016MatrixRoomEnergySimulationResultMapper, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016MatrixRoomEnergySimulationResultMapper>();
        services.AddScoped<IBuildingEnergyCalculator, Iso52016BuildingEnergyCalculator>();

        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.ISo52016PhysicalRoomModelBuilder, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalRoomModelBuilder>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.ISo52016PhysicalRoomEnergySimulationService, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalRoomEnergySimulationService>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.ISo52016PhysicalModelSelectionService, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalModelSelectionService>();

        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.ISo52016PhysicalRoomModelDiagnosticsBuilder, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalRoomModelDiagnosticsBuilder>();

        return services;
    }
}
