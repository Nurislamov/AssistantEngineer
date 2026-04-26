using AssistantEngineer.Api.Controllers.Buildings;
using AssistantEngineer.Api.Controllers.Reports;
using AssistantEngineer.Modules.Reporting.Application.Facades;

namespace AssistantEngineer.Tests.Architecture;

public class ReportControllerResponsibilityTests
{
    [Fact]
    public void CoolingReportControllerDependsOnlyOnCoolingReportsFacade()
    {
        AssertControllerHasSingleDependency<BuildingCoolingReportsController, IBuildingCoolingReportsFacade>();
    }

    [Fact]
    public void HeatingReportControllerDependsOnlyOnHeatingReportsFacade()
    {
        AssertControllerHasSingleDependency<BuildingHeatingReportsController, IBuildingHeatingReportsFacade>();
    }

    [Fact]
    public void EnergyBalanceReportControllerDependsOnlyOnEnergyBalanceReportsFacade()
    {
        AssertControllerHasSingleDependency<BuildingEnergyBalanceReportsController, IBuildingEnergyBalanceReportsFacade>();
    }

    [Fact]
    public void SingleGenericReportsControllerDoesNotExist()
    {
        var controller = typeof(BuildingsController).Assembly
            .GetTypes()
            .FirstOrDefault(type =>
                string.Equals(
                    type.Name,
                    "ReportsController",
                    StringComparison.Ordinal));

        Assert.Null(controller);
    }

    private static void AssertControllerHasSingleDependency<TController, TDependency>()
    {
        var constructor = Assert.Single(typeof(TController).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(TDependency), parameter.ParameterType);
    }
}