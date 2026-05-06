using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class GroundRegistration
{
    public static IServiceCollection AddGroundCalculations(
        this IServiceCollection services)
    {
        services.AddScoped<GroundTemperatureProfilePreviewService>();
        services.AddScoped<Iso13370GroundTemperatureProfileCalculator>();
        services.AddScoped<Iso13370GroundBoundaryCalculator>();
        services.AddScoped<Iso13370GroundBoundaryApplicationAdapter>();

        services.AddSingleton<IGroundTemperatureService, Iso13370GroundTemperatureService>();
        services.AddSingleton<IGroundHeatTransferService, Iso13370GroundHeatTransferService>();

        return services;
    }
}
