using AssistantEngineer.Api.Controllers.Buildings;
using AssistantEngineer.Api.Controllers.Reports;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Reporting.Application.Facades;

namespace AssistantEngineer.Tests.Architecture;

public class ReportControllerResponsibilityTests
{
    [Fact]
    public void CoolingReportControllerDependsOnlyOnCoolingReportsFacade()
    {
        AssertControllerHasDependencies<BuildingCoolingReportsController>(
            typeof(IBuildingCoolingReportsFacade),
            typeof(IProtectedEndpointAuthorizationGate));
    }

    [Fact]
    public void HeatingReportControllerDependsOnlyOnHeatingReportsFacade()
    {
        AssertControllerHasDependencies<BuildingHeatingReportsController>(
            typeof(IBuildingHeatingReportsFacade),
            typeof(IProtectedEndpointAuthorizationGate));
    }

    [Fact]
    public void EnergyBalanceReportControllerDependsOnlyOnEnergyBalanceReportsFacade()
    {
        AssertControllerHasDependencies<BuildingEnergyBalanceReportsController>(
            typeof(IBuildingEnergyBalanceReportsFacade),
            typeof(IProtectedEndpointAuthorizationGate));
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

    private static void AssertControllerHasDependencies<TController>(params Type[] expectedDependencies)
    {
        var constructor = Assert.Single(typeof(TController).GetConstructors());
        var actual = constructor.GetParameters()
            .Select(parameter => parameter.ParameterType)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        var expected = expectedDependencies
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }
}
