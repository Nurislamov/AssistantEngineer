using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Services.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class CalculationTraceRegistration
{
    public static IServiceCollection AddCalculationTraceFoundation(
        this IServiceCollection services)
    {
        services.AddTransient<ICalculationTraceBuilder, CalculationTraceBuilder>();
        services.AddSingleton<ICalculationTraceSanitizer, CalculationTraceSanitizer>();
        services.AddSingleton<ICalculationTraceJsonExporter, CalculationTraceJsonExporter>();
        services.AddSingleton<ICalculationTraceDiagnosticMapper, CalculationTraceDiagnosticMapper>();
        services.AddSingleton<ICalculationTraceModuleAdapter, CalculationTraceModuleAdapter>();

        return services;
    }
}
