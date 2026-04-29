using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Simplified;
using AssistantEngineer.Modules.Calculations.Application.Services.Floors;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class LoadCalculationRegistration
{
    public static IServiceCollection AddCoolingLoadCalculations(
        this IServiceCollection services)
    {
        services.TryAddSingleton<TransmissionHeatTransferEngine>();
        services.AddScoped<IRoomCoolingLoadCalculationStrategy, SimplifiedCoolingLoadCalculator>();
        services.AddScoped<IRoomCoolingLoadCalculationStrategy, Iso52016CoolingLoadCalculator>();

        services.AddScoped<IRoomCoolingLoadCalculator, RoomCoolingLoadCalculator>();
        services.AddScoped<IAggregateLoadCalculator, AggregateCalculator>();

        services.AddScoped<BuildingCoolingLoadService>();
        services.AddScoped<FloorCalculationService>();
        services.AddScoped<RoomCalculationService>();

        return services;
    }

    public static IServiceCollection AddHeatingLoadCalculations(
        this IServiceCollection services)
    {
        services.TryAddSingleton<TransmissionHeatTransferEngine>();
        services.AddScoped<En12831HeatingLoadCalculator>();
        services.AddScoped<BuildingHeatingReadModelCalculator>();

        services.AddScoped<IRoomHeatingLoadCalculator>(sp =>
            sp.GetRequiredService<En12831HeatingLoadCalculator>());

        services.AddScoped<IBuildingHeatingLoadCalculator>(sp =>
            sp.GetRequiredService<En12831HeatingLoadCalculator>());

        services.AddScoped<BuildingHeatingLoadService>();

        return services;
    }
}
