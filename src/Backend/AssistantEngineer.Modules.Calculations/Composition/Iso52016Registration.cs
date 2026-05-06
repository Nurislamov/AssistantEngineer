using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class Iso52016Registration
{
    public static IServiceCollection AddIso52016Calculations(
        this IServiceCollection services)
    {
        services.AddScoped<IIso52016ReferenceDataProvider, Iso52016ReferenceDataProvider>();
        services.AddScoped<Iso52016ClimateDataValidator>();

        services.AddScoped<ISolarRadiationService, SolarRadiationService>();
        services.AddScoped<IWindowShadingService, WindowShadingService>();

        services.AddScoped<IIso52016GroundBoundaryTemperatureProvider, PeriodicIso52016GroundBoundaryTemperatureProvider>();
        services.AddScoped<IIso52016WeatherSolarContextBuilder, Iso52016WeatherSolarContextBuilder>();

        services.AddSingleton<Iso52016ConstructionReferenceDataProvider>();
        services.AddSingleton<Iso52016ConstructionAssemblyCalculator>();
        services.AddSingleton<Iso52016ConstructionAssemblyApplicationAdapter>();

        services.AddScoped<IIso52016WindowSolarGainCalculator, Iso52016WindowSolarGainCalculator>();
        services.AddScoped<IIso52016WindowSolarGainProfileBuilder, Iso52016WindowSolarGainProfileBuilder>();
        services.AddScoped<IIso52016RoomSolarGainProfileBuilder, Iso52016RoomSolarGainProfileBuilder>();

        services.AddScoped<InternalGainEngine>();
        services.AddScoped<IIso52016RoomInternalGainProfileBuilder, Iso52016RoomInternalGainProfileBuilder>();

        services.AddScoped<IIso52016RoomHourlyInputProfileBuilder, Iso52016RoomHourlyInputProfileBuilder>();
        services.AddScoped<IIso52016RoomEnergySimulationService, Iso52016RoomEnergySimulationService>();

        services.AddScoped<IIso52016ScheduleProfileExpander, Iso52016ScheduleProfileExpander>();
        services.AddScoped<IIso52016RoomEnvelopeInputCalculator, Iso52016RoomEnvelopeInputCalculator>();
        services.AddScoped<IIso52016RoomWindowSolarGainInputMapper, Iso52016RoomWindowSolarGainInputMapper>();
        services.AddScoped<IIso52016RoomEnergySimulationRequestBuilder, Iso52016RoomEnergySimulationRequestBuilder>();

        services.AddScoped<IIso52016RoomSimulationFacade, Iso52016RoomSimulationFacade>();
        services.AddScoped<IIso52016BuildingSimulationFacade, Iso52016BuildingSimulationFacade>();

        services.AddScoped<IIso52016BuildingRoomCollector, Iso52016BuildingRoomCollector>();
        services.AddScoped<IIso52016BuildingDomainSimulationFacade, Iso52016BuildingDomainSimulationFacade>();

        services.AddScoped<IIso52016BuildingEnergySimulationApplicationService, Iso52016BuildingEnergySimulationApplicationService>();

        services.AddScoped<Iso52016HourlySteadyStateCalculator>();
        services.AddScoped<Iso52016MonthlyQuasiSteadyStateCalculator>();

        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.IIso52016MatrixHourlySolver, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016MatrixHourlySolver>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.IIso52016InternalGainReferenceDataProvider, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016InternalGainReferenceDataProvider>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.IIso52016AdjacentUnconditionedZoneTemperatureSolver, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016AdjacentUnconditionedZoneTemperatureSolver>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.IIso52016MatrixReducedRoomModelBuilder, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016MatrixReducedRoomModelBuilder>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.IIso52016PhysicalRoomModelBuilder, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalRoomModelBuilder>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.IIso52016MatrixRoomEnergySimulationService, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016MatrixRoomEnergySimulationService>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix.IIso52016MatrixRoomEnergySimulationResultMapper, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix.Iso52016MatrixRoomEnergySimulationResultMapper>();
        services.AddScoped<IBuildingEnergyCalculator, Iso52016BuildingEnergyCalculator>();

        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.IIso52016PhysicalRoomModelBuilder, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalRoomModelBuilder>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.IIso52016PhysicalRoomEnergySimulationService, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalRoomEnergySimulationService>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.IIso52016PhysicalModelSelectionService, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalModelSelectionService>();

        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical.IIso52016PhysicalRoomModelDiagnosticsBuilder, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical.Iso52016PhysicalRoomModelDiagnosticsBuilder>();

        return services;
    }
}
