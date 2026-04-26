using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class GroundRegistration
{
    public static IServiceCollection AddGroundCalculations(
        this IServiceCollection services)
    {
        services.AddScoped<GroundTemperatureProfilePreviewService>();

        services.AddSingleton<IGroundTemperatureService, Iso13370GroundTemperatureService>();
        services.AddSingleton<IGroundHeatTransferService, Iso13370GroundHeatTransferService>();

        return services;
    }
}