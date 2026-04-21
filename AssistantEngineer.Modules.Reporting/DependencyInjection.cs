using AssistantEngineer.Modules.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<BuildingReportCalculationService>();
        services.AddScoped<BuildingReportGenerator>();
        services.AddScoped<BuildingReportDataService>();

        return services;
    }
}
