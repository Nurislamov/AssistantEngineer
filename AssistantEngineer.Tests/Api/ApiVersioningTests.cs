using AssistantEngineer.Api.Controllers;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AssistantEngineer.Tests;

public class ApiVersioningTests
{
    [Theory]
    [InlineData(typeof(BenchmarksController), "api/v{version:apiVersion}/benchmarks")]
    [InlineData(typeof(BuildingArchetypesController), "api/v{version:apiVersion}/building-archetypes")]
    [InlineData(typeof(BuildingEnergyAnalysisController), "api/v{version:apiVersion}/buildings/{buildingId:int}/energy-analysis")]
    [InlineData(typeof(BuildingReadinessController), "api/v{version:apiVersion}/buildings/{buildingId:int}/readiness")]
    [InlineData(typeof(BuildingsController), "api/v{version:apiVersion}/buildings")]
    [InlineData(typeof(AnnualClimateDataController), "api/v{version:apiVersion}/climate-zones/{climateZoneId:int}/annual-climate-data")]
    [InlineData(typeof(DomesticHotWaterController), "api/v{version:apiVersion}/domestic-hot-water")]
    [InlineData(typeof(EquipmentCatalogController), "api/v{version:apiVersion}/equipment-catalog")]
    [InlineData(typeof(FloorsController), "api/v{version:apiVersion}/floors")]
    [InlineData(typeof(ProjectsController), "api/v{version:apiVersion}/projects")]
    [InlineData(typeof(ReportsController), "api/v{version:apiVersion}/reports")]
    [InlineData(typeof(RoomsController), "api/v{version:apiVersion}/rooms")]
    public void ControllersDeclareExplicitV1UrlSegmentRoute(
        Type controllerType,
        string versionedRoute)
    {
        var routes = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Contains(versionedRoute, routes);
        Assert.DoesNotContain(routes, route => route is not null && !route.Contains("{version:apiVersion}"));

        var apiVersions = controllerType
            .GetCustomAttributes(typeof(ApiVersionAttribute), inherit: false)
            .Cast<ApiVersionAttribute>()
            .SelectMany(attribute => attribute.Versions)
            .ToArray();

        Assert.Contains(apiVersions, version => version.MajorVersion == 1 && version.MinorVersion == 0);
    }

    [Theory]
    [InlineData(
        typeof(BuildingsController),
        nameof(BuildingsController.Create),
        "~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    [InlineData(
        typeof(BuildingsController),
        nameof(BuildingsController.GetByProject),
        "~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    [InlineData(
        typeof(FloorsController),
        nameof(FloorsController.Create),
        "~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    [InlineData(
        typeof(FloorsController),
        nameof(FloorsController.GetByBuilding),
        "~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    public void NestedResourceActionsUseParentFirstRoutes(
        Type controllerType,
        string actionName,
        string routeTemplate)
    {
        var action = controllerType.GetMethod(actionName);
        Assert.NotNull(action);

        var routes = action
            .GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
            .Cast<HttpMethodAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Contains(routeTemplate, routes);
    }
}
