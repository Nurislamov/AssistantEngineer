using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Reporting.Composition;

internal static class EngineeringReportFoundationRegistration
{
    public static IServiceCollection AddEngineeringReportFoundationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IEngineeringReportDiagnosticAggregator, EngineeringReportDiagnosticAggregator>();
        services.AddScoped<IEngineeringReportBuilder, EngineeringReportBuilder>();
        services.AddScoped<IEngineeringReportJsonExporter, EngineeringReportJsonExporter>();
        services.AddScoped<IEngineeringReportMarkdownExporter, EngineeringReportMarkdownExporter>();

        return services;
    }
}

