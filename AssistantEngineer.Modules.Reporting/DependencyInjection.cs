using AssistantEngineer.Modules.Reporting.Application.Facades;
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
        services.AddScoped<IReportsFacade>(sp => new ReportsFacade(
            sp.GetRequiredService<BuildingReportDataService>(),
            sp.GetRequiredService<AssistantEngineer.Modules.Reporting.Application.Abstractions.IBuildingReportExporter>(),
            sp.GetRequiredService<AssistantEngineer.Modules.Calculations.Application.Services.Buildings.BuildingEnergyBalanceService>()));

        return services;
    }
}
