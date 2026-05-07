using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class ThermalTopologyRegistration
{
    public static IServiceCollection AddThermalTopologyFoundation(
        this IServiceCollection services)
    {
        services.AddSingleton<IThermalTopologyBuilder, ThermalTopologyBuilder>();
        services.AddSingleton<IThermalBoundaryConditionResolver, ThermalBoundaryConditionResolver>();
        services.AddSingleton<IThermalTopologyValidator, ThermalTopologyValidator>();

        return services;
    }
}
