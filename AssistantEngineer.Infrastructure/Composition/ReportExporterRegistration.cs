using AssistantEngineer.Infrastructure.Integrations.Reports.Excel;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Composition;

internal static class ReportExporterRegistration
{
    public static IServiceCollection AddReportExporters(
        this IServiceCollection services)
    {
        services.AddScoped<IBuildingCoolingReportExporter, BuildingCoolingExcelReportExporter>();
        services.AddScoped<IBuildingEnergyBalanceReportExporter, BuildingEnergyBalanceExcelReportExporter>();

        return services;
    }
}