using AssistantEngineer.Modules.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Reporting.Composition;

internal static class ReportGeneratorRegistration
{
    public static IServiceCollection AddReportingGenerators(
        this IServiceCollection services)
    {
        services.AddScoped<BuildingCoolingReportGenerator>();
        services.AddScoped<BuildingHeatingReportGenerator>();

        return services;
    }
}