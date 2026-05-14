using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Simplified;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
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
        services.TryAddScoped<RoomLoadCalculationEngine>();
        services.AddScoped<IRoomCoolingLoadCalculationStrategy, SimplifiedCoolingLoadCalculator>();
        services.AddScoped<IRoomCoolingLoadCalculationStrategy, Iso52016CoolingLoadCalculator>();

        services.AddScoped<IRoomCoolingLoadCalculator, RoomCoolingLoadCalculator>();
        services.AddScoped<IAggregateLoadCalculator, AggregateCalculator>();
        services.TryAddSingleton<LoadAggregationEngine>();

        services.AddScoped<IEquipmentSizingCalculationUseCase, EquipmentSizingCalculationUseCase>();
        services.AddScoped<ISystemEnergyHandoffUsefulDemandProvider, PipelineSystemEnergyHandoffUsefulDemandProvider>();
        services.AddScoped<ISystemEnergyHandoffUseCase, SystemEnergyHandoffUseCase>();
        services.AddScoped<EnergyCalculationPipelineService>();
        services.AddScoped<IEnergyCalculationPipeline>(sp =>
            sp.GetRequiredService<EnergyCalculationPipelineService>());

        return services;
    }

    public static IServiceCollection AddHeatingLoadCalculations(
        this IServiceCollection services)
    {
        services.TryAddSingleton<TransmissionHeatTransferEngine>();
        services.TryAddScoped<RoomLoadCalculationEngine>();
        services.AddScoped<En12831HeatingLoadCalculator>();
        services.AddScoped<BuildingHeatingReadModelCalculator>();

        services.AddScoped<IRoomHeatingLoadCalculator>(sp =>
            sp.GetRequiredService<En12831HeatingLoadCalculator>());

        services.AddScoped<IBuildingHeatingLoadCalculator>(sp =>
            sp.GetRequiredService<En12831HeatingLoadCalculator>());

        return services;
    }
}

