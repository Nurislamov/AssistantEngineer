using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
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

        services.AddScoped<IIso52016WindowSolarGainCalculator, Iso52016WindowSolarGainCalculator>();
        services.AddScoped<IIso52016WindowSolarGainProfileBuilder, Iso52016WindowSolarGainProfileBuilder>();
        services.AddScoped<IIso52016RoomSolarGainProfileBuilder, Iso52016RoomSolarGainProfileBuilder>();

        services.AddScoped<InternalGainEngine>();
        services.AddScoped<IIso52016RoomInternalGainProfileBuilder, Iso52016RoomInternalGainProfileBuilder>();

        services.AddScoped<IIso52016RoomHourlyInputProfileBuilder, Iso52016RoomHourlyInputProfileBuilder>();
        services.AddScoped<IIso52016RoomHeatBalanceSolver, Iso52016RoomHeatBalanceSolver>();
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

        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2.IIso52016V2HourlySolver, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2.Iso52016V2HourlySolver>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2.IIso52016InternalGainReferenceDataProvider, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2.Iso52016InternalGainReferenceDataProvider>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2.IIso52016AdjacentUnconditionedZoneTemperatureSolver, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2.Iso52016AdjacentUnconditionedZoneTemperatureSolver>();
        services.AddScoped<IBuildingEnergyCalculator, Iso52016BuildingEnergyCalculator>();

        return services;
    }
}