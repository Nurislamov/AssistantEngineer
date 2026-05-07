using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class GroundCalculationRegistration
{
    public static IServiceCollection AddGroundBoundaryTopologyIntegration(
        this IServiceCollection services)
    {
        services.AddSingleton<IGroundBoundaryTopologyMapper, GroundBoundaryTopologyMapper>();
        services.AddSingleton<IBuildingGroundBoundaryCalculator, BuildingGroundBoundaryCalculator>();
        services.AddSingleton<IGroundBoundaryTemperatureLookupBuilder, GroundBoundaryTemperatureLookupBuilder>();
        services.AddSingleton<IThermalZoneGroundBoundaryInputAdapter, ThermalZoneBoundaryGroundTemperatureAdapter>();
        services.AddSingleton<IGroundBoundaryToIso52016BoundaryProfileMapper, GroundBoundaryToIso52016BoundaryProfileMapper>();

        return services;
    }
}
