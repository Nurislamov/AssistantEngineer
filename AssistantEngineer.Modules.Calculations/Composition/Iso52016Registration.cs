using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
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

        services.AddScoped<Iso52016HourlySteadyStateCalculator>();
        services.AddScoped<Iso52016MonthlyQuasiSteadyStateCalculator>();

        services.AddScoped<IBuildingEnergyCalculator, Iso52016BuildingEnergyCalculator>();

        return services;
    }
}