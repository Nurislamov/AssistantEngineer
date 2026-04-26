using AssistantEngineer.Infrastructure;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;

namespace AssistantEngineer.Tests.Architecture;

public class InfrastructureReportExportStructureTests
{
    [Fact]
    public void GenericExcelReportServiceDoesNotExist()
    {
        var type = typeof(DependencyInjection).Assembly
            .GetType("AssistantEngineer.Infrastructure.Integrations.Reports.ExcelReportService");

        Assert.Null(type);
    }

    [Fact]
    public void InfrastructureUsesSpecializedExcelReportExporters()
    {
        var typeNames = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == "AssistantEngineer.Infrastructure.Integrations.Reports.Excel")
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("BuildingCoolingExcelReportExporter", typeNames);
        Assert.Contains("BuildingEnergyBalanceExcelReportExporter", typeNames);
        Assert.Contains("ExcelWorkbookWriter", typeNames);
    }

    [Fact]
    public void SpecializedExcelExportersImplementSpecializedReportingPorts()
    {
        var assembly = typeof(DependencyInjection).Assembly;

        var coolingExporter = assembly.GetType(
            "AssistantEngineer.Infrastructure.Integrations.Reports.Excel.BuildingCoolingExcelReportExporter");

        var energyBalanceExporter = assembly.GetType(
            "AssistantEngineer.Infrastructure.Integrations.Reports.Excel.BuildingEnergyBalanceExcelReportExporter");

        Assert.NotNull(coolingExporter);
        Assert.NotNull(energyBalanceExporter);

        Assert.True(typeof(IBuildingCoolingReportExporter).IsAssignableFrom(coolingExporter));
        Assert.True(typeof(IBuildingEnergyBalanceReportExporter).IsAssignableFrom(energyBalanceExporter));
    }
}