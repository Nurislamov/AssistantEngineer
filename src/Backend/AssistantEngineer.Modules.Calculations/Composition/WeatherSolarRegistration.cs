using AssistantEngineer.Modules.Calculations.Application.Abstractions.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class WeatherSolarRegistration
{
    public static IServiceCollection AddWeatherSolarCalculations(
        this IServiceCollection services)
    {
        services.AddSingleton<IAnnualWeatherSolarProfileBuilder, AnnualWeatherSolarProfileBuilder>();

        return services;
    }
}