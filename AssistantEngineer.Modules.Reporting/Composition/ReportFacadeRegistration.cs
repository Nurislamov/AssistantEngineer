using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Reporting.Composition;

internal static class ReportFacadeRegistration
{
    public static IServiceCollection AddReportingFacades(
        this IServiceCollection services)
    {
        services.AddScoped<IBuildingCoolingReportsFacade>(sp => new BuildingCoolingReportsFacade(
            sp.GetRequiredService<BuildingCoolingReportDataService>(),
            sp.GetRequiredService<IBuildingCoolingReportExporter>()));

        services.AddScoped<IBuildingHeatingReportsFacade>(sp => new BuildingHeatingReportsFacade(
            sp.GetRequiredService<BuildingHeatingReportDataService>()));

        services.AddScoped<IBuildingEnergyBalanceReportsFacade>(sp => new BuildingEnergyBalanceReportsFacade(
            sp.GetRequiredService<ILoadCalculationsFacade>(),
            sp.GetRequiredService<IBuildingEnergyBalanceReportExporter>()));

        return services;
    }
}