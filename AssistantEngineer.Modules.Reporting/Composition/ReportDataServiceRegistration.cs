using AssistantEngineer.Modules.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Reporting.Composition;

internal static class ReportDataServiceRegistration
{
    public static IServiceCollection AddReportingDataServices(
        this IServiceCollection services)
    {
        services.AddScoped<BuildingCoolingReportDataService>();
        services.AddScoped<BuildingHeatingReportDataService>();

        return services;
    }
}