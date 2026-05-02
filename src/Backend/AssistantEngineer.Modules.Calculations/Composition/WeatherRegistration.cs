using AssistantEngineer.Modules.Calculations.Application.Abstractions.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class WeatherRegistration
{
    public static IServiceCollection AddWeatherCalculations(
        this IServiceCollection services)
    {
        services.AddSingleton<IAnnualWeatherDataNormalizer, AnnualClimateDataNormalizer>();

        return services;
    }
}