using AssistantEngineer.Modules.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Reporting.Composition;

internal static class ReportCalculationRegistration
{
    public static IServiceCollection AddReportingCalculationServices(
        this IServiceCollection services)
    {
        services.AddScoped<BuildingCoolingReportCalculationService>();
        services.AddScoped<BuildingHeatingReportCalculationService>();

        return services;
    }
}