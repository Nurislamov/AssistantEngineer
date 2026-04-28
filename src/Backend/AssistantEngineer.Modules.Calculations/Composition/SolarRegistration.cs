using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class SolarRegistration
{
    public static IServiceCollection AddSolarCalculations(
        this IServiceCollection services)
    {
        services.AddSingleton<ISolarPositionCalculator, SolarPositionCalculator>();
        services.AddSingleton<ISurfaceIrradianceCalculator, IsotropicSkySurfaceIrradianceCalculator>();

        return services;
    }
}