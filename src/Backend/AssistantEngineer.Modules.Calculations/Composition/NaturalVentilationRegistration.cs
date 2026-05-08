using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class NaturalVentilationRegistration
{
    public static IServiceCollection AddNaturalVentilationFoundation(
        this IServiceCollection services)
    {
        services.AddSingleton<INaturalVentilationOpeningGeometryNormalizer, NaturalVentilationOpeningGeometryNormalizer>();
        services.AddSingleton<INaturalVentilationInputValidator, NaturalVentilationInputValidator>();
        services.AddSingleton<INaturalVentilationPressureCalculator, NaturalVentilationPressureCalculator>();
        services.AddSingleton<INaturalVentilationAirflowCalculator, NaturalVentilationAirflowCalculator>();
        services.AddSingleton<INaturalVentilationControlRuleValidator, NaturalVentilationControlRuleValidator>();
        services.AddSingleton<INaturalVentilationOpeningControlEvaluator, NaturalVentilationOpeningControlEvaluator>();
        services.AddSingleton<INaturalVentilationOpeningFractionProfileBuilder, NaturalVentilationOpeningFractionProfileBuilder>();
        services.AddSingleton<INaturalVentilationControlledAirflowInputBuilder, NaturalVentilationControlledAirflowInputBuilder>();

        return services;
    }
}
